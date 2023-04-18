using AutoMapper;

using FluentValidation;

using Js.LedgerEs.Cqrs;
using Js.LedgerEs.EventSourcing;

namespace Js.LedgerEs.Ledgers;

public sealed record JournalPaymentRequestBody(
    string Description,
    decimal Amount
);

public sealed record JournalPayment(
    Guid LedgerId,
    string Description,
    decimal Amount
) : ICommand<PaymentJournalled>
{
    public Guid GetStreamUniqueIdentifier() => LedgerId;
}

public sealed class JournalPaymentValidator : AbstractValidator<JournalPayment>
{
    public JournalPaymentValidator()
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

public sealed class JournalPaymentHandler : AbstractCommandHandler<JournalPayment, PaymentJournalled, LedgerWriteModel>
{
    public JournalPaymentHandler(IMapper mapper, IEventClient eventClient) : base(mapper, eventClient)
    {
    }
}

public sealed record PaymentJournalled(
    Guid LedgerId,
    string Description,
    decimal Amount
) : SerializableEvent
{
    public override Guid GetStreamUniqueIdentifier() => LedgerId;
}
