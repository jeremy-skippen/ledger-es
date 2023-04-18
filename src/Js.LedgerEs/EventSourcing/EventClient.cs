using System.Reflection;
using System.Text.Json;

using EventStore.Client;

namespace Js.LedgerEs.EventSourcing;

/// <summary>
/// The client interface to the event store.
/// </summary>
public interface IEventClient
{
    /// <summary>
    /// Get the current state of the write model for a stream by opening the stream and aggregating all events.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the write model to create.
    /// </typeparam>
    /// <param name="streamId">
    /// The stream name / id to aggregate.
    /// </param>
    /// <param name="cancellationToken">
    /// Cancellation token.
    /// </param>
    /// <returns>
    /// The aggregated write model.
    /// </returns>
    Task<T?> AggregateStream<T>(
        string streamId,
        CancellationToken cancellationToken
    ) where T : class, IWriteModel, new();

    /// <summary>
    /// Append an event to a stream in the event store.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the event to write to the store.
    /// </typeparam>
    /// <param name="streamName">
    /// The stream name / id to write to.
    /// </param>
    /// <param name="aggregate">
    /// The write model the event relates to.
    /// </param>
    /// <param name="expectedVersion">
    /// The stream version expected. If the stream version in the store is different from the expected version a
    /// <see cref="EventStoreConcurrencyException"/> will be thrown.
    /// </param>
    /// <param name="event">
    /// The event to write to the store.
    /// </param>
    /// <param name="cancellationToken">
    /// Cancellation token.
    /// </param>
    /// <exception cref="EventStoreConcurrencyException">
    /// Thrown when the expected stream version does not match the actual stream version in the event store.
    /// </exception>
    Task AppendToStreamAsync<T>(
        string streamName,
        IWriteModel aggregate,
        ulong expectedVersion,
        T @event,
        CancellationToken cancellationToken
    )
        where T : class, ISerializableEvent;

    /// <summary>
    /// Get the stream name for an aggregate with the given unique identifier.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the write model the stream represents.
    /// </typeparam>
    /// <param name="streamId">
    /// The stream unique identifier.
    /// </param>
    /// <returns>
    /// The stream name.
    /// </returns>
    string GetStreamNameForAggregate<T>(Guid streamId) where T : class, IWriteModel;

    /// <summary>
    /// Subscribe to the "All" stream in the event store.
    /// This is particularly useful for writing read models / aggregates to a separate data store.
    /// </summary>
    /// <param name="fromPosition">
    /// Where to read the all stream from. Set to <c>0</c> to read from the beginning.
    /// </param>
    /// <param name="onEventAppeared">
    /// Callback invoked when a new event appears on the all stream.
    /// </param>
    /// <param name="onSubscriptionDropped">
    /// Callback invoked if the subscription is dropped for any reason.
    /// </param>
    /// <param name="cancellationToken">
    /// Cancellation token.
    /// </param>
    Task SubscribeToAllStreamAsync(
        ulong fromPosition,
        Func<ISerializableEvent?, ulong, CancellationToken, Task> onEventAppeared,
        Action<string, Exception?> onSubscriptionDropped,
        CancellationToken cancellationToken
    );
}

public sealed class EventClient : IEventClient
{
    private readonly EventStoreClient _eventStore;
    private readonly IEnumerable<SerializableEventRegistration> _knownEvents;
    private readonly JsonSerializerOptions _jsonOptions;

    public EventClient(
        EventStoreClient eventStore,
        IEnumerable<SerializableEventRegistration> knownEvents,
        JsonSerializerOptions jsonOptions
    )
    {
        _eventStore = eventStore;
        _knownEvents = knownEvents;
        _jsonOptions = jsonOptions;
    }

    public async Task<T?> AggregateStream<T>(
        string streamId,
        CancellationToken cancellationToken
    ) where T : class, IWriteModel, new()
    {
        var readResult = _eventStore.ReadStreamAsync(
            Direction.Forwards,
            streamId,
            StreamPosition.Start,
            cancellationToken: cancellationToken
        );

        var readState = await readResult.ReadState;
        if (readState == ReadState.StreamNotFound)
            return null;

        var aggregate = new T();

        await foreach (var @event in readResult)
        {
            var eventData = DeserializeFromResolvedEvent(@event);

            aggregate.Apply(eventData);
        }

        return aggregate;
    }

    public async Task AppendToStreamAsync<T>(
        string streamName,
        IWriteModel aggregate,
        ulong expectedVersion,
        T @event,
        CancellationToken cancellationToken
    )
        where T : class, ISerializableEvent
    {
        try
        {
            await _eventStore.AppendToStreamAsync(
                streamName,
                expectedVersion == 0
                    ? StreamRevision.None
                    : expectedVersion - 1,
                new[] { SerializeToEventData(@event) },
                cancellationToken: cancellationToken
            );
        }
        catch (WrongExpectedVersionException ex)
        {
            throw new EventStoreConcurrencyException(
                aggregate,
                @event,
                expectedVersion,
                ex.ActualVersion.HasValue
                    ? (ulong)ex.ActualVersion + 1
                    : 0,
                ex.Message
            );
        }
    }

    private ISerializableEvent? DeserializeFromResolvedEvent(ResolvedEvent resolvedEvent)
    {
        var type = GetEventTypeByName(resolvedEvent.Event.EventType);
        if (type == null || !type.IsAssignableTo(typeof(ISerializableEvent)))
            return null;

        if (JsonSerializer.Deserialize(resolvedEvent.Event.Data.Span, type, _jsonOptions) is not ISerializableEvent @event)
            return null;

        @event.EventId = resolvedEvent.Event.EventId.ToGuid();
        @event.EventDateTime = new DateTimeOffset(resolvedEvent.Event.Created);

        return @event;
    }

    private EventData SerializeToEventData<T>(T @event) where T : class, ISerializableEvent
        => new(
            Uuid.FromGuid(@event.EventId),
            GetEventNameByType(@event.GetType()) ?? "UnknownEvent",
            JsonSerializer.SerializeToUtf8Bytes(@event, _jsonOptions),
            JsonSerializer.SerializeToUtf8Bytes(new { })
        );

    public string GetStreamNameForAggregate<T>(Guid streamId) where T : class, IWriteModel
    {
        var attr = typeof(T).GetCustomAttribute<StreamNameFormatAttribute>();
        if (attr is null)
            return $"{typeof(T).Name.ToLowerInvariant()}-{streamId:d}";

        return string.Format(attr.Format, streamId);
    }

    private Type? GetEventTypeByName(string name)
        => _knownEvents.Where(r => r.Name == name).Select(r => r.Type).SingleOrDefault();

    private string? GetEventNameByType(Type type)
        => _knownEvents.Where(r => r.Type == type).Select(r => r.Name).SingleOrDefault();

    public Task SubscribeToAllStreamAsync(
        ulong fromPosition,
        Func<ISerializableEvent?, ulong, CancellationToken, Task> onEventAppeared,
        Action<string, Exception?> onSubscriptionDropped,
        CancellationToken cancellationToken
    ) => _eventStore.SubscribeToAllAsync(
            fromPosition == 0
                ? FromAll.Start
                : FromAll.After(new Position(fromPosition, fromPosition)),
            eventAppeared: (StreamSubscription _, ResolvedEvent resolvedEvent, CancellationToken ct) => onEventAppeared(
                DeserializeFromResolvedEvent(resolvedEvent),
                resolvedEvent.Event.Position.CommitPosition,
                ct
            ),
            subscriptionDropped: (StreamSubscription _, SubscriptionDroppedReason reason, Exception? exception) => onSubscriptionDropped(
                reason switch
                {
                    SubscriptionDroppedReason.Disposed => "the subscription was disposed",
                    SubscriptionDroppedReason.SubscriberError => "there is an error in user code",
                    SubscriptionDroppedReason.ServerError => "there was a server error",
                    _ => "unknown reason",
                },
                exception
            ),
            cancellationToken: cancellationToken
        );
}
