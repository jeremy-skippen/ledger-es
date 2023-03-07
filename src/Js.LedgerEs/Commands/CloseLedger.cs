using AutoMapper;

using EventStore.Client;

using FluentValidation;

using Js.LedgerEs.EventSourcing;

using MediatR;

namespace Js.LedgerEs.Commands;

public sealed record CloseLedger(
    Guid LedgerId
) : ICommand,
    IRequest<LedgerClosed>
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
    public CloseLedgerHandler(IMapper mapper, EventStoreClient eventStore) : base(mapper, eventStore)
    {
    }
}

public sealed record LedgerClosed(
    Guid LedgerId
) : SerializableEvent
{
    public override Guid GetStreamUniqueIdentifier() => LedgerId;
}
