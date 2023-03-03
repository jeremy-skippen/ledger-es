using EventStore.Client;

namespace Js.LedgerEs.EventSourcing;

/// <summary>
/// <para>
/// Represents a read or write model as described
/// <a href="https://www.eventstore.com/event-sourcing#Projections">here</a>.
/// </para>
/// <para>
/// Implementers of this interface should be projections or aggregates capable of calculating their state by applying
/// all historical events from the relevant stream in order.
/// </para>
/// </summary>
public interface IAggregate
{
    /// <summary>
    /// Contains the aggregate revision.
    /// This can be calculated in 2 ways:
    /// <list type="number">
    /// <item>Replaying the history of events for a stream on an empty aggregate - this is typical when an aggregate
    /// is required to validate preconditions before committing a new event to the event store</item>
    /// <item>Deserialized from the read model - this would happen when recalculating a read model after an event has
    /// been committed to the event store</item>
    /// </list>
    /// This value is used for optimistic concurrency when writing to the event store and when projecting the read
    /// model to the read database.
    /// </summary>
    ulong Version { get; }

    /// <summary>
    /// Applies an event to the aggregate. If the event is relevant to the aggregate the state will change and
    /// <see cref="Version" /> will be incremented.
    /// </summary>
    /// <param name="event">
    /// The event to apply.
    /// </param>
    void Apply(object? @event);
}

public interface IEventHandler
{
}

public interface IEventHandler<T> : IEventHandler where T : class, ISerializableEvent
{
    void Apply(T @event);
}

public static class AggregateStreamExtensions
{
    public static async Task<T?> AggregateStream<T>(
        this EventStoreClient eventStore,
        string streamId,
        CancellationToken cancellationToken,
        ulong? fromVersion = null
    ) where T : class, IAggregate, new()
    {
        var readResult = eventStore.ReadStreamAsync(
            Direction.Forwards,
            streamId,
            fromVersion ?? StreamPosition.Start,
            cancellationToken: cancellationToken
        );

        var readState = await readResult.ReadState;
        if (readState == ReadState.StreamNotFound)
            return null;

        var aggregate = new T();

        await foreach (var @event in readResult)
        {
            var eventData = @event.DeserializeFromResolvedEvent();

            aggregate.Apply(eventData);
        }

        return aggregate;
    }
}
