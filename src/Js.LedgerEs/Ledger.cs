using Js.LedgerEs.Commands;

namespace Js.LedgerEs;

public sealed class Ledger :
    IAggregate,
    IEventHandler<LedgerOpened>,
    IEventHandler<ReceiptJournalled>,
    IEventHandler<LedgerClosed>
{
    public enum JournalType
    {
        Journal,
        Payment,
        Receipt,
    };

    public sealed record JournalEntry(
        Guid EntryId,
        string Description,
        decimal Amount,
        JournalType Type,
        Guid? ReversesEntryId = null,
        Guid? RelatedLedgerId = null,
        Guid? RelatedEntryId = null
    );

    public Guid LedgerId { get; private set; }

    public string LedgerName { get; private set; }

    public bool Open { get; private set; }

    public IList<JournalEntry> Entries { get; }

    public decimal Balance { get; private set; }

    public ulong Version { get; private set; }

    public Ledger()
    {
        LedgerId = Guid.Empty;
        LedgerName = string.Empty;
        Open = false;
        Balance = 0;
        Entries = new List<JournalEntry>();
        Version = ulong.MaxValue;
    }

    public void Apply(object? @event)
    {
        switch (@event)
        {
            case LedgerOpened opened:
                Handle(opened);
                break;
            case ReceiptJournalled receipted:
                Handle(receipted);
                break;
            case PaymentJournalled payment:
                Handle(payment);
                break;
            case LedgerClosed closed:
                Handle(closed);
                break;
        };
    }

    public void Handle(LedgerOpened @event)
    {
        if (Open)
            throw new InvalidStateTransitionException(this, @event, "Cannot open a ledger that is already opened");

        LedgerId = @event.LedgerId;
        LedgerName = @event.LedgerName;
        Open = true;

        // Handle the case of an empty object - needed as stream revisions start at 0
        if (Version == ulong.MaxValue)
            Version = 0;
        else
            Version += 1;
    }

    public void Handle(ReceiptJournalled @event)
    {
        if (!Open)
            throw new InvalidStateTransitionException(this, @event, "Cannot receipt to a closed ledger");

        Entries.Add(new JournalEntry(@event.EventId, @event.Description, @event.Amount, JournalType.Receipt));
        Balance += @event.Amount;
        Version += 1;
    }

    public void Handle(PaymentJournalled @event)
    {
        if (!Open)
            throw new InvalidStateTransitionException(this, @event, "Cannot pay from a closed ledger");
        if (Balance < @event.Amount)
            throw new InvalidStateTransitionException(this, @event, $"Ledger has insufficient balance - ${Balance:f2}", nameof(@event.Amount));

        Entries.Add(new JournalEntry(@event.EventId, @event.Description, @event.Amount, JournalType.Payment));
        Balance -= @event.Amount;
        Version += 1;
    }

    public void Handle(LedgerClosed @event)
    {
        if (!Open)
            throw new InvalidStateTransitionException(this, @event, "Cannot close a ledger that is not open");
        if (Balance != 0)
            throw new InvalidStateTransitionException(this, @event, "Cannot close a ledger that has balance");

        Open = false;
        Version += 1;
    }
}
