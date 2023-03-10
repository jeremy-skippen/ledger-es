using System.Text.Json;

using Js.LedgerEs.Configuration;

using MediatR;

namespace Js.LedgerEs.Ledgers;

public sealed record GetLedger(
    Guid LedgerId
) : IRequest<GetLedgerResponse>;

public sealed class GetLedgerRequestHandler : IRequestHandler<GetLedger, GetLedgerResponse>
{
    private readonly IMediator _mediator;

    public GetLedgerRequestHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<GetLedgerResponse> Handle(GetLedger request, CancellationToken cancellationToken)
    {
        LedgerReadModel? ledger = null;

        var jsonResponse = await _mediator.Send(new GetLedgerRawJson(request.LedgerId), cancellationToken);
        if (jsonResponse.Ledger is not null)
        {
            ledger = JsonSerializer.Deserialize<LedgerReadModel>(jsonResponse.Ledger, JsonConfig.SerializerOptions);
        }

        return new GetLedgerResponse(ledger);
    }
}

public sealed record GetLedgerResponse(LedgerReadModel? Ledger);
