namespace Js.LedgerEs.EventSourcing;

/// <summary>
/// Represents an error that occurs when attempting to apply an event to an aggregate that violates domain rules.
/// </summary>
public class InvalidStateTransitionException : LedgerEsException
{
    public override int HttpStatusCode => 400;

    /// <summary>
    /// The aggregate that was the target of the event.
    /// </summary>
    public IAggregate Aggregate { get; }

    /// <summary>
    /// The event that could not be applied to the aggregate.
    /// </summary>
    public ISerializableEvent Event { get; }

    /// <summary>
    /// If specified, the name of the property of the event that caused the domain rule violation.
    /// </summary>
    public string? EventProperty { get; }

    public InvalidStateTransitionException(
        IAggregate aggregate,
        ISerializableEvent @event,
        string? message,
        string? eventProperty = null
    ) : base(message)
    {
        Aggregate = aggregate;
        Event = @event;
        EventProperty = eventProperty;
    }
}

/// <summary>
/// Represents an error that occurs when attempting to serialize an event to the event store when the stream version
/// and the expected version don't match.
/// </summary>
public class EventStoreConcurrencyException : LedgerEsException
{
    public override int HttpStatusCode => 409;

    /// <summary>
    /// The aggregate that relates to the stream that could not be written to.
    /// </summary>
    public IAggregate Aggregate { get; }

    /// <summary>
    /// The event that could not be serialized.
    /// </summary>
    public ISerializableEvent Event { get; }

    /// <summary>
    /// The stream version that was expected.
    /// </summary>
    public ulong ExpectedStreamVersion { get; }

    /// <summary>
    /// The actual stream version.
    /// </summary>
    public ulong ActualStreamVersion { get; }

    public EventStoreConcurrencyException(
        IAggregate aggregate,
        ISerializableEvent @event,
        ulong expectedStreamVersion,
        ulong actualStreamVerion,
        string? message
    ) : base(message)
    {
        Aggregate = aggregate;
        Event = @event;
        ExpectedStreamVersion = expectedStreamVersion;
        ActualStreamVersion = actualStreamVerion;
    }
}
