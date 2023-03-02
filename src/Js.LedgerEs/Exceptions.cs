using static EventStore.Client.StreamMessage;

namespace Js.LedgerEs;

public class LedgerEsException : Exception
{
    public LedgerEsException(string? message) : base(message)
    {
    }
}

public class InvalidStateTransitionException : LedgerEsException
{
    public IAggregate Aggregate { get; }

    public ISerializableEvent Event { get; }

    public string? EventProperty { get; }

    public InvalidStateTransitionException(IAggregate aggregate, ISerializableEvent @event, string? message, string? eventProperty = null) : base(message)
    {
        Aggregate = aggregate;
        Event = @event;
        EventProperty = eventProperty;
    }
}

public class EventStoreConcurrencyException : LedgerEsException
{
    public IAggregate Aggregate { get; }

    public EventStoreConcurrencyException(IAggregate aggregate, string? message) : base(message)
    {
        Aggregate = aggregate;
    }
}
