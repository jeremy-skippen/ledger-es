using System.Text.Json.Serialization;

namespace Js.LedgerEs.EventSourcing;

public interface ISerializableEvent
{
    Guid EventId { get; set; }
    DateTimeOffset EventDateTime { get; set; }

    Guid GetStreamUniqueIdentifier();
}

public abstract record SerializableEvent : ISerializableEvent
{
    [JsonIgnore]
    public Guid EventId { get; set; }

    [JsonIgnore]
    public DateTimeOffset EventDateTime { get; set; }

    public abstract Guid GetStreamUniqueIdentifier();
}

public sealed record SerializableEventRegistration(
    string Name,
    Type Type
);
