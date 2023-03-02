using EventStore.Client;

namespace Js.LedgerEs.EventSourcing;

public static class AggregateStreamExtensions
{
    public static async Task<T?> AggregateStream<T>(
        this EventStoreClient eventStore,
        string streamId,
        CancellationToken cancellationToken,
        ulong? fromVersion = null
    ) where T : class, IAggregate
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

        var aggregate = Activator.CreateInstance<T>();

        await foreach (var @event in readResult)
        {
            var eventData = @event.DeserializeFromResolvedEvent();

            aggregate.Apply(eventData);
        }

        return aggregate;
    }
}
