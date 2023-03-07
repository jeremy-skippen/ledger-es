using AutoMapper;

using FluentValidation;

using Js.LedgerEs.EventSourcing;

using MediatR;

namespace Js.LedgerEs.Ledgers;

public sealed record OpenLedger(
    Guid LedgerId,
    string LedgerName
) : ICommand,
    IRequest<LedgerOpened>
{
    public Guid GetStreamUniqueIdentifier() => LedgerId;
}

public sealed class OpenLedgerValidator : AbstractValidator<OpenLedger>
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

public sealed class OpenLedgerHandler : AbstractCommandHandler<OpenLedger, LedgerOpened, LedgerWriteModel>
{
    public OpenLedgerHandler(IMapper mapper, IEventClient eventClient) : base(mapper, eventClient)
    {
    }
}

public sealed record LedgerOpened(
    Guid LedgerId,
    string LedgerName
) : SerializableEvent
{
    public override Guid GetStreamUniqueIdentifier() => LedgerId;
}
