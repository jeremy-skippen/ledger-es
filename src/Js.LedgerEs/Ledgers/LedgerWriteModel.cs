﻿using Js.LedgerEs.EventSourcing;

namespace Js.LedgerEs.Ledgers;

[StreamNameFormat("ledger-{0:d}")]
public sealed class LedgerWriteModel : IWriteModel
{
    public Guid LedgerId { get; private set; }

    public bool IsOpen { get; private set; }

    public decimal Balance { get; private set; }

    public ulong Version { get; private set; }

    public LedgerWriteModel()
    {
        LedgerId = Guid.Empty;
        IsOpen = false;
        Balance = 0;
        Version = 0;
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
        IsOpen = true;

        Version += 1;
    }

    public void JournalReceipt(ReceiptJournalled @event)
    {
        if (!IsOpen)
            throw new InvalidStateTransitionException(this, @event, "Cannot receipt to a closed ledger");

        Balance += @event.Amount;
        Version += 1;
    }

    public void JournalPayment(PaymentJournalled @event)
    {
        if (!IsOpen)
            throw new InvalidStateTransitionException(this, @event, "Cannot pay from a closed ledger");
        if (Balance < @event.Amount)
            throw new InvalidStateTransitionException(this, @event, $"Ledger has insufficient balance - ${Balance:f2}", nameof(@event.Amount));

        Balance -= @event.Amount;
        Version += 1;
    }

    public void Close(LedgerClosed @event)
    {
        if (!IsOpen)
            throw new InvalidStateTransitionException(this, @event, "Cannot close a ledger that is not open");
        if (Balance != 0)
            throw new InvalidStateTransitionException(this, @event, "Cannot close a ledger that has balance");

        IsOpen = false;
        Version += 1;
    }
}
