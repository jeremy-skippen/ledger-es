using AutoMapper;

using EventStore.Client;

using FluentValidation;

using Js.LedgerEs.EventSourcing;

using MediatR;

namespace Js.LedgerEs.Commands;

public sealed record JournalReceiptRequestBody(
    string Description,
    decimal Amount
);

public sealed record JournalReceipt(
    Guid LedgerId,
    string Description,
    decimal Amount
) : ICommand,
    IRequest<ReceiptJournalled>
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
    public JournalReceiptHandler(IMapper mapper, EventStoreClient eventStore) : base(mapper, eventStore)
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
