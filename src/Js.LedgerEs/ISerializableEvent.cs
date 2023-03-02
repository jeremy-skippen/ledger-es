namespace Js.LedgerEs;

public interface ISerializableEvent
{
    Guid EventId { get; }
    DateTimeOffset EventDateTime { get; }
}
