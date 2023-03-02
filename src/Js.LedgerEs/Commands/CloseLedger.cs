using AutoMapper;

using EventStore.Client;

using FluentValidation;

using MediatR;

namespace Js.LedgerEs.Commands;

public record CloseLedger(
    Guid LedgerId
) : ICommand,
    IRequest<LedgerClosed>
{
    public Guid GetStreamUniqueIdentifier() => LedgerId;
}

public class CloseLedgerValidator : AbstractValidator<CloseLedger>
{
    public CloseLedgerValidator()
    {
        RuleFor(r => r.LedgerId)
            .NotEmpty();
    }
}

public sealed class CloseLedgerHandler : AbstractRequestHandler<CloseLedger, LedgerClosed, Ledger>
{
    public CloseLedgerHandler(IMapper mapper, EventStoreClient eventStore) : base(mapper, eventStore)
    {
    }
}

public record LedgerClosed(
    Guid EventId,
    DateTimeOffset EventDateTime,
    Guid LedgerId
) : ISerializableEvent;
