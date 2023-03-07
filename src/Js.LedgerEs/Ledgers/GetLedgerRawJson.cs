using Dapper;

using FluentValidation;

using Js.LedgerEs.Configuration;

using MediatR;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Js.LedgerEs.Ledgers;

public sealed record GetLedgerRawJson(
    Guid LedgerId
) : IRequest<GetLedgerRawJsonResponse>;

public sealed class GetLedgerRawJsonValidator : AbstractValidator<GetLedgerRawJson>
{
    public GetLedgerRawJsonValidator()
    {
        RuleFor(r => r.LedgerId)
            .NotEmpty();
    }
}

public sealed class GetLedgerRawJsonRequestHandler : IRequestHandler<GetLedgerRawJson, GetLedgerRawJsonResponse>
{
    private readonly IOptions<LedgerEsConfiguration> _cfg;

    private const string QUERY = @"
        SELECT
            ledgerId,
            ledgerName,
            isOpen,
            entries = JSON_QUERY(Entries, '$'),
            balance,
            [version],
            modifiedDate
        FROM dbo.LedgerView
        WHERE LedgerId = @LedgerId
        FOR JSON AUTO, WITHOUT_ARRAY_WRAPPER
    ";

    public GetLedgerRawJsonRequestHandler(IOptions<LedgerEsConfiguration> cfg)
    {
        _cfg = cfg;
    }

    public async Task<GetLedgerRawJsonResponse> Handle(GetLedgerRawJson request, CancellationToken cancellationToken)
    {
        using var conn = new SqlConnection(_cfg.Value.SqlServerConnectionString);

        return new GetLedgerRawJsonResponse(await conn.ExecuteScalarAsync<string>(QUERY, request));
    }
}

public sealed record GetLedgerRawJsonResponse(string? Ledger);
