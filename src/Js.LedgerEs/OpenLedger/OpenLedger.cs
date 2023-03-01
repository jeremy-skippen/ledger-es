using MediatR;

namespace Js.LedgerEs.OpenLedger;

public record OpenLedger(
    string LedgerId,
    string LedgerName
) : IRequest<LedgerOpened>;
