using AutoMapper;

namespace Js.LedgerEs.Ledgers;

public class LedgerCommandToSerializableEventMappingProfile : Profile
{
    public LedgerCommandToSerializableEventMappingProfile()
    {
        CreateMap<OpenLedger, LedgerOpened>()
            .ConstructUsing(r => new LedgerOpened(r.LedgerId, r.LedgerName));

        CreateMap<JournalReceipt, ReceiptJournalled>()
            .ConstructUsing(r => new ReceiptJournalled(r.LedgerId, r.Description, r.Amount));

        CreateMap<JournalPayment, PaymentJournalled>()
            .ConstructUsing(r => new PaymentJournalled(r.LedgerId, r.Description, r.Amount));

        CreateMap<CloseLedger, LedgerClosed>()
            .ConstructUsing(r => new LedgerClosed(r.LedgerId));
    }
}
