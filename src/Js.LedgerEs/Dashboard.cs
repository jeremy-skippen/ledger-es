﻿using System.Text.Json.Serialization;

using Js.LedgerEs.Commands;
using Js.LedgerEs.EventSourcing;

namespace Js.LedgerEs;

public sealed class Dashboard : IAggregate
{
    public int LedgerCount { get; private set; }
    public int LedgerOpenCount { get; private set; }
    public int LedgerClosedCount { get; private set; }

    public int TransactionCount { get; private set; }
    public int ReceiptCount { get; private set; }
    public int PaymentCount { get; private set; }

    public decimal NetAmount { get; private set; }
    public decimal ReceiptAmount { get; private set; }
    public decimal PaymentAmount { get; private set; }

    public ulong Version { get; private set; }
    public DateTimeOffset ModifiedDate { get; private set; }

    public Dashboard()
    {
        LedgerCount = 0;
        LedgerOpenCount = 0;
        LedgerClosedCount = 0;

        TransactionCount = 0;
        ReceiptCount = 0;
        PaymentCount = 0;

        NetAmount = 0;
        ReceiptAmount = 0;
        PaymentAmount = 0;

        Version = ulong.MaxValue;
        ModifiedDate = DateTimeOffset.MinValue;
    }

    [JsonConstructor]
    public Dashboard(
        int LedgerCount,
        int LedgerOpenCount,
        int LedgerClosedCount,
        int TransactionCount,
        int ReceiptCount,
        int PaymentCount,
        decimal NetAmount,
        decimal ReceiptAmount,
        decimal PaymentAmount,
        ulong Version,
        DateTimeOffset ModifiedDate
    )
    {
        this.LedgerCount = LedgerCount;
        this.LedgerOpenCount = LedgerOpenCount;
        this.LedgerClosedCount = LedgerClosedCount;
        this.TransactionCount = TransactionCount;
        this.ReceiptCount = ReceiptCount;
        this.PaymentCount = PaymentCount;
        this.NetAmount = NetAmount;
        this.ReceiptAmount = ReceiptAmount;
        this.PaymentAmount = PaymentAmount;
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
        LedgerCount += 1;
        LedgerOpenCount += 1;

        Version += 1;
        ModifiedDate = @event.EventDateTime;
    }

    public void JournalReceipt(ReceiptJournalled @event)
    {
        TransactionCount += 1;
        ReceiptCount += 1;

        NetAmount += @event.Amount;
        ReceiptAmount += @event.Amount;

        Version += 1;
        ModifiedDate = @event.EventDateTime;
    }

    public void JournalPayment(PaymentJournalled @event)
    {
        TransactionCount += 1;
        PaymentCount += 1;

        NetAmount -= @event.Amount;
        PaymentAmount += @event.Amount;

        Version += 1;
        ModifiedDate = @event.EventDateTime;
    }

    public void Close(LedgerClosed @event)
    {
        LedgerCount -= 1;
        LedgerClosedCount += 1;

        Version += 1;
        ModifiedDate = @event.EventDateTime;
    }
}
