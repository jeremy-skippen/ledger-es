using System.Text.Json.Serialization;

using Js.LedgerEs.Commands;
using Js.LedgerEs.EventSourcing;

namespace Js.LedgerEs;

public sealed class Ledger : IAggregate
{
    public enum JournalType
    {
        Receipt,
        Payment,
    };

    public sealed record JournalEntry(
        Guid EntryId,
        string Description,
        decimal Amount,
        JournalType Type,
        DateTimeOffset JournalDate
    );

    public Guid LedgerId { get; private set; }

    public string LedgerName { get; private set; }

    public bool IsOpen { get; private set; }

    public IList<JournalEntry> Entries { get; }

    public decimal Balance { get; private set; }

    public ulong Version { get; private set; }

    public DateTimeOffset ModifiedDate { get; private set; }

    public Ledger()
    {
        LedgerId = Guid.Empty;
        LedgerName = string.Empty;
        IsOpen = false;
        Entries = new List<JournalEntry>();
        Balance = 0;
        Version = ulong.MaxValue;
        ModifiedDate = DateTimeOffset.MinValue;
    }

    [JsonConstructor]
    public Ledger(
        Guid LedgerId,
        string LedgerName,
        bool IsOpen,
        IList<JournalEntry> Entries,
        decimal Balance,
        ulong Version,
        DateTimeOffset ModifiedDate
    )
    {
        this.LedgerId = LedgerId;
        this.LedgerName = LedgerName;
        this.IsOpen = IsOpen;
        this.Entries = Entries;
        this.Balance = Balance;
        this.Version = Version;
        this.ModifiedDate = ModifiedDate;
    }

    public void Apply(ISerializableEvent? @event)
    {
        switch (@event)
        {
            case LedgerOpened opened:
                Open(opened);
                break;
            case ReceiptJournalled receipted:
                JournalReceipt(receipted);
                break;
            case PaymentJournalled payment:
                JournalPayment(payment);
                break;
            case LedgerClosed closed:
                Close(closed);
                break;
        };
    }

    public void Open(LedgerOpened @event)
    {
        if (IsOpen)
            throw new InvalidStateTransitionException(this, @event, "Cannot open a ledger that is already opened");

        LedgerId = @event.LedgerId;
        LedgerName = @event.LedgerName;
        IsOpen = true;

        // Handle the case of an empty object - needed as stream revisions start at 0
        if (Version == ulong.MaxValue)
            Version = 0;
        else
            Version += 1;

        ModifiedDate = @event.EventDateTime;
    }

    public void JournalReceipt(ReceiptJournalled @event)
    {
        if (!IsOpen)
            throw new InvalidStateTransitionException(this, @event, "Cannot receipt to a closed ledger");

        Entries.Add(new JournalEntry(
            @event.EventId,
            @event.Description,
            @event.Amount,
            JournalType.Receipt,
            @event.EventDateTime
        ));
        Balance += @event.Amount;
        Version += 1;
        ModifiedDate = @event.EventDateTime;
    }

    public void JournalPayment(PaymentJournalled @event)
    {
        if (!IsOpen)
            throw new InvalidStateTransitionException(this, @event, "Cannot pay from a closed ledger");
        if (Balance < @event.Amount)
            throw new InvalidStateTransitionException(this, @event, $"Ledger has insufficient balance - ${Balance:f2}", nameof(@event.Amount));

        Entries.Add(new JournalEntry(
            @event.EventId,
            @event.Description,
            -@event.Amount,
            JournalType.Payment,
            @event.EventDateTime
        ));
        Balance -= @event.Amount;
        Version += 1;
        ModifiedDate = @event.EventDateTime;
    }

    public void Close(LedgerClosed @event)
    {
        if (!IsOpen)
            throw new InvalidStateTransitionException(this, @event, "Cannot close a ledger that is not open");
        if (Balance != 0)
            throw new InvalidStateTransitionException(this, @event, "Cannot close a ledger that has balance");

        IsOpen = false;
        Version += 1;
        ModifiedDate = @event.EventDateTime;
    }
}
