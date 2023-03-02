using AutoMapper;

using EventStore.Client;

using FluentValidation;

using MediatR;

namespace Js.LedgerEs.Commands;

public record JournalPaymentRequestBody(
    string Description,
    decimal Amount
);

public record JournalPayment(
    Guid LedgerId,
    string Description,
    decimal Amount
) : ICommand,
    IRequest<PaymentJournalled>
{
    public Guid GetStreamUniqueIdentifier() => LedgerId;
}

public class JournalPaymentValidator : AbstractValidator<JournalPayment>
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

public sealed class JournalPaymentHandler : AbstractRequestHandler<JournalPayment, PaymentJournalled, Ledger>
{
    public JournalPaymentHandler(IMapper mapper, EventStoreClient eventStore) : base(mapper, eventStore)
    {
    }
}

public record PaymentJournalled(
    Guid EventId,
    DateTimeOffset EventDateTime,
    Guid LedgerId,
    string Description,
    decimal Amount
) : ISerializableEvent;
