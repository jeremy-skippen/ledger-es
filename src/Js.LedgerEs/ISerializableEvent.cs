using EventStore.Client;

namespace Js.LedgerEs;

public interface ISerializableEvent
{
    Uuid EventId { get; }
}
