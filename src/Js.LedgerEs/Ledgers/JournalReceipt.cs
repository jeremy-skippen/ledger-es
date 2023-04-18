using AutoMapper;

using FluentValidation;

using Js.LedgerEs.Cqrs;
using Js.LedgerEs.EventSourcing;

namespace Js.LedgerEs.Ledgers;

public sealed record JournalReceiptRequestBody(
    string Description,
    decimal Amount
);

public sealed record JournalReceipt(
    Guid LedgerId,
    string Description,
    decimal Amount
) : ICommand<ReceiptJournalled>
{
    public Guid GetStreamUniqueIdentifier() => LedgerId;
}

public sealed class JournalReceiptValidator : AbstractValidator<JournalReceipt>
{
    public JournalReceiptValidator()
    {
        RuleFor(r => r.LedgerId)
            .NotEmpty();

        RuleFor(r => r.Description)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(r => r.Amount)
            .GreaterThan(0);
    }
}

public sealed class JournalReceiptHandler : AbstractCommandHandler<JournalReceipt, ReceiptJournalled, LedgerWriteModel>
{
    public JournalReceiptHandler(IMapper mapper, IEventClient eventClient) : base(mapper, eventClient)
    {
    }
}

public sealed record ReceiptJournalled(
    Guid LedgerId,
    string Description,
    decimal Amount
) : SerializableEvent
{
    public override Guid GetStreamUniqueIdentifier() => LedgerId;
}
