using AutoMapper;

using Js.LedgerEs.Commands;

namespace Js.LedgerEs;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<OpenLedger, LedgerOpened>()
            .ConstructUsing(
                (r, ctx) => ctx.TryGetItems(out var runtimeValues)
                    ? new LedgerOpened(
                        (Guid)runtimeValues[nameof(LedgerOpened.EventId)],
                        (DateTimeOffset)runtimeValues[nameof(LedgerOpened.EventDateTime)],
                        r.LedgerId,
                        r.LedgerName
                    )
                    : throw new Exception("Can't map between OpenLedger and LedgerOpened without runtime event values")
            );

        CreateMap<JournalReceipt, ReceiptJournalled>()
            .ConstructUsing(
                (r, ctx) => ctx.TryGetItems(out var runtimeValues)
                    ? new ReceiptJournalled(
                        (Guid)runtimeValues[nameof(ReceiptJournalled.EventId)],
                        (DateTimeOffset)runtimeValues[nameof(ReceiptJournalled.EventDateTime)],
                        r.LedgerId,
                        r.Description,
                        r.Amount
                    )
                    : throw new Exception("Can't map between JournalReceipt and ReceiptJournalled without runtime event values")
            );

        CreateMap<JournalPayment, PaymentJournalled>()
            .ConstructUsing(
                (r, ctx) => ctx.TryGetItems(out var runtimeValues)
                    ? new PaymentJournalled(
                        (Guid)runtimeValues[nameof(PaymentJournalled.EventId)],
                        (DateTimeOffset)runtimeValues[nameof(PaymentJournalled.EventDateTime)],
                        r.LedgerId,
                        r.Description,
                        r.Amount
                    )
                    : throw new Exception("Can't map between JournalPayment and PaymentJournalled without runtime event values")
            );

        CreateMap<CloseLedger, LedgerClosed>()
            .ConstructUsing(
                (r, ctx) => ctx.TryGetItems(out var runtimeValues)
                    ? new LedgerClosed(
                        (Guid)runtimeValues[nameof(LedgerClosed.EventId)],
                        (DateTimeOffset)runtimeValues[nameof(LedgerClosed.EventDateTime)],
                        r.LedgerId
                    )
                    : throw new Exception("Can't map between CloseLedger and LedgerClosed without runtime event values")
            );
    }
}
