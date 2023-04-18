namespace Js.LedgerEs.EventSourcing;

/// <summary>
/// Represents an aggregate used to validate a command before commiting an event to the event store.
/// </summary>
public interface IWriteModel : IAggregate
{
}
