using AutoMapper;

using EventStore.Client;

using FluentValidation;

using MediatR;

namespace Js.LedgerEs.Commands;

public record OpenLedger(
    Guid LedgerId,
    string LedgerName
) : ICommand,
    IRequest<LedgerOpened>
{
    public Guid GetStreamUniqueIdentifier() => LedgerId;
}

public class OpenLedgerValidator : AbstractValidator<OpenLedger>
{
    public OpenLedgerValidator()
    {
        RuleFor(r => r.LedgerId)
            .NotEmpty();

        RuleFor(r => r.LedgerName)
            .NotEmpty()
            .MaximumLength(255);
    }
}

public sealed class OpenLedgerHandler : AbstractRequestHandler<OpenLedger, LedgerOpened, Ledger>
{
    public OpenLedgerHandler(IMapper mapper, EventStoreClient eventStore) : base(mapper, eventStore)
    {
    }
}

public record LedgerOpened(
    Guid EventId,
    DateTimeOffset EventDateTime,
    Guid LedgerId,
    string LedgerName
) : ISerializableEvent;
