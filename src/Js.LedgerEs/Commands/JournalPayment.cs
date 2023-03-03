using AutoMapper;

using EventStore.Client;

using FluentValidation;

using Js.LedgerEs.EventSourcing;

using MediatR;

namespace Js.LedgerEs.Commands;

public sealed record JournalPaymentRequestBody(
    string Description,
    decimal Amount
);

public sealed record JournalPayment(
    Guid LedgerId,
    string Description,
    decimal Amount
) : ICommand,
    IRequest<PaymentJournalled>
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

public sealed class JournalPaymentHandler : AbstractCommandHandler<JournalPayment, PaymentJournalled, Ledger>
{
    public JournalPaymentHandler(IMapper mapper, EventStoreClient eventStore) : base(mapper, eventStore)
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
