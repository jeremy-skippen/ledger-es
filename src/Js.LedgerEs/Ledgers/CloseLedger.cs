using AutoMapper;

using FluentValidation;

using Js.LedgerEs.Cqrs;
using Js.LedgerEs.EventSourcing;

namespace Js.LedgerEs.Ledgers;

public sealed record CloseLedger(
    Guid LedgerId
) : ICommand<LedgerClosed>
{
    public Guid GetStreamUniqueIdentifier() => LedgerId;
}

public sealed class CloseLedgerValidator : AbstractValidator<CloseLedger>
{
    public CloseLedgerValidator()
    {
        RuleFor(r => r.LedgerId)
            .NotEmpty();
    }
}

public sealed class CloseLedgerHandler : AbstractCommandHandler<CloseLedger, LedgerClosed, LedgerWriteModel>
{
    public CloseLedgerHandler(IMapper mapper, IEventClient eventClient) : base(mapper, eventClient)
    {
    }
}

public sealed record LedgerClosed(
    Guid LedgerId
) : SerializableEvent
{
    public override Guid GetStreamUniqueIdentifier() => LedgerId;
}
