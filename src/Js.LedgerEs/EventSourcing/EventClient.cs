using System.Reflection;
using System.Text.Json;

using EventStore.Client;

using Js.LedgerEs.Configuration;

namespace Js.LedgerEs.EventSourcing;

public interface IEventClient
{
    Task<T?> AggregateStream<T>(
        string streamId,
        CancellationToken cancellationToken
    ) where T : class, IWriteModel, new();

    Task AppendToStreamAsync<T>(
        string streamName,
        IWriteModel aggregate,
        StreamRevision expectedVersion,
        T @event,
        CancellationToken cancellationToken
    )
        where T : class, ISerializableEvent;

    ISerializableEvent? DeserializeFromResolvedEvent(ResolvedEvent resolvedEvent);

    string GetStreamNameForAggregate<T>(Guid streamId) where T : class, IWriteModel;

    Task<StreamSubscription> SubscribeToAllAsync(
        FromAll start,
        Func<StreamSubscription, ResolvedEvent, CancellationToken, Task> eventAppeared,
        Action<StreamSubscription, SubscriptionDroppedReason, Exception?>? subscriptionDropped,
        CancellationToken cancellationToken
    );
}

public sealed class EventClient : IEventClient
{
    private readonly EventStoreClient _eventStore;
    private readonly IEnumerable<SerializableEventRegistration> _knownEvents;

    public EventClient(
        EventStoreClient eventStore,
        IEnumerable<SerializableEventRegistration> knownEvents
    )
    {
        _eventStore = eventStore;
        _knownEvents = knownEvents;
    }

    public async Task<T?> AggregateStream<T>(string streamId, CancellationToken cancellationToken) where T : class, IWriteModel, new()
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
        StreamRevision expectedVersion,
        T @event,
        CancellationToken cancellationToken
    )
        where T : class, ISerializableEvent
    {
        try
        {
            await _eventStore.AppendToStreamAsync(
                streamName,
                expectedVersion,
                new[] { SerializeToEventData(@event) },
                cancellationToken: cancellationToken
            );
        }
        catch (WrongExpectedVersionException ex)
        {
            throw new EventStoreConcurrencyException(aggregate, ex.Message);
        }
    }

    public ISerializableEvent? DeserializeFromResolvedEvent(ResolvedEvent resolvedEvent)
    {
        var type = GetEventTypeByName(resolvedEvent.Event.EventType);
        if (type == null || !type.IsAssignableTo(typeof(ISerializableEvent)))
            return null;

        if (JsonSerializer.Deserialize(resolvedEvent.Event.Data.Span, type, JsonConfig.SerializerOptions) is not ISerializableEvent @event)
            return null;

        @event.EventId = resolvedEvent.Event.EventId.ToGuid();
        @event.EventDateTime = new DateTimeOffset(resolvedEvent.Event.Created);

        return @event;
    }

    private EventData SerializeToEventData<T>(T @event) where T : class, ISerializableEvent
        => new(
            Uuid.FromGuid(@event.EventId),
            GetEventNameByType(@event.GetType()) ?? "UnknownEvent",
            JsonSerializer.SerializeToUtf8Bytes(@event, JsonConfig.SerializerOptions),
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

    public Task<StreamSubscription> SubscribeToAllAsync(
        FromAll start,
        Func<StreamSubscription, ResolvedEvent, CancellationToken, Task> eventAppeared,
        Action<StreamSubscription, SubscriptionDroppedReason, Exception?>? subscriptionDropped,
        CancellationToken cancellationToken
    ) => _eventStore.SubscribeToAllAsync(
            start,
            eventAppeared: eventAppeared,
            subscriptionDropped: subscriptionDropped,
            cancellationToken: cancellationToken
        );
}
