using AutoMapper;

using EventStore.Client;

using FluentValidation;

using MediatR;

namespace Js.LedgerEs.Commands;

public record JournalReceiptRequestBody(
    string Description,
    decimal Amount
);

public record JournalReceipt(
    Guid LedgerId,
    string Description,
    decimal Amount
) : ICommand,
    IRequest<ReceiptJournalled>
{
    public Guid GetStreamUniqueIdentifier() => LedgerId;
}

public class JournalReceiptValidator : AbstractValidator<JournalReceipt>
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

public sealed class JournalReceiptHandler : AbstractRequestHandler<JournalReceipt, ReceiptJournalled, Ledger>
{
    public JournalReceiptHandler(IMapper mapper, EventStoreClient eventStore) : base(mapper, eventStore)
    {
    }
}

public record ReceiptJournalled(
    Guid EventId,
    DateTimeOffset EventDateTime,
    Guid LedgerId,
    string Description,
    decimal Amount
) : ISerializableEvent;
