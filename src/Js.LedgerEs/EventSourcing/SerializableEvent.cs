using System.Text.Json.Serialization;

namespace Js.LedgerEs.EventSourcing;

/// <summary>
/// Represents an event (in the event sourcing sense) that can be serialized to the event store.
/// </summary>
public interface ISerializableEvent
{
    /// <summary>
    /// The unique event identifier.
    /// </summary>
    Guid EventId { get; set; }
    /// <summary>
    /// The date/time the event occurred.
    /// </summary>
    DateTimeOffset EventDateTime { get; set; }

    /// <summary>
    /// Get the unique identifier of the stream this event belongs to.
    /// </summary>
    /// <returns>
    /// The unique identifier of the stream this event belongs to.
    /// </returns>
    Guid GetStreamUniqueIdentifier();
}

/// <summary>
/// <para>
/// Abstract implementation of <see cref="ISerializableEvent"/>.
/// </para>
/// <para>
/// When serializing events that inherit from this base class <see cref="EventId"/> and <see cref="EventDateTime"/>
/// will automatically be omitted from the serialized JSON. These fields do not need to be serialized in the event
/// store as they are captured separately in the event metadata.
/// </para>
/// </summary>
public abstract record SerializableEvent : ISerializableEvent
{
    [JsonIgnore]
    public Guid EventId { get; set; }

    [JsonIgnore]
    public DateTimeOffset EventDateTime { get; set; }

    public abstract Guid GetStreamUniqueIdentifier();
}

/// <summary>
/// This type is used to map event names as stored in the event store to concrete event types in the application.
/// These records are created when the application starts using reflection, and as such this type is reserved for
/// internal use only.
/// </summary>
/// <param name="Name">
/// The event type name as stored in the event store.
/// </param>
/// <param name="Type">
/// The C# type of the event.
/// </param>
public sealed record SerializableEventRegistration(
    string Name,
    Type Type
);
