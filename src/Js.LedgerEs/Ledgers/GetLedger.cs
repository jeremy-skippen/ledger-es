using System.Text.Json;

using MediatR;

namespace Js.LedgerEs.Ledgers;

public sealed record GetLedger(
    Guid LedgerId
) : IRequest<GetLedgerResponse>;

public sealed class GetLedgerRequestHandler : IRequestHandler<GetLedger, GetLedgerResponse>
{
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly IMediator _mediator;

    public GetLedgerRequestHandler(
        JsonSerializerOptions jsonOptions,
        IMediator mediator
    )
    {
        _jsonOptions = jsonOptions;
        _mediator = mediator;
    }

    public async Task<GetLedgerResponse> Handle(GetLedger request, CancellationToken cancellationToken)
    {
        LedgerViewModel? ledger = null;

        var jsonResponse = await _mediator.Send(new GetLedgerRawJson(request.LedgerId), cancellationToken);
        if (jsonResponse.Ledger is not null)
        {
            ledger = JsonSerializer.Deserialize<LedgerViewModel>(jsonResponse.Ledger, _jsonOptions);
        }

        return new GetLedgerResponse(ledger);
    }
}

public sealed record GetLedgerResponse(LedgerViewModel? Ledger);
