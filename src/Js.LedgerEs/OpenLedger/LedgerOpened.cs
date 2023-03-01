using EventStore.Client;

using System.Text.Json.Serialization;

namespace Js.LedgerEs.OpenLedger;

public record LedgerOpened(
    string LedgerId,
    string LedgerName,
    DateTimeOffset OpenedAt
) : ISerializableEvent
{
    [JsonIgnore]
    public Uuid EventId { get; init; }
}
