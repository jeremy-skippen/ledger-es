using Dapper;

using FluentValidation;

using Js.LedgerEs.Configuration;

using MediatR;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Js.LedgerEs.Requests;

public sealed record GetLedgerList(
    int? Page,
    int? PageSize
) : IRequest<GetLedgerListResponse>;

public sealed class GetLedgerListValidator : AbstractValidator<GetLedgerList>
{
    public GetLedgerListValidator()
    {
        RuleFor(r => r.Page)
            .GreaterThanOrEqualTo(0);
        RuleFor(r => r.PageSize)
            .GreaterThan(0)
            .LessThanOrEqualTo(200);
    }
}

public sealed class GetLedgerListRequestHandler : IRequestHandler<GetLedgerList, GetLedgerListResponse>
{
    private readonly IOptions<LedgerEsConfiguration> _cfg;

    private const string QUERY = @"
        SELECT
            LedgerId,
            LedgerName,
            IsOpen,
            Balance,
            ModifiedDate
        FROM dbo.LedgerView
        ORDER BY Id
        OFFSET @offset ROWS
        FETCH NEXT @pageSize ROWS ONLY
    ";

    public GetLedgerListRequestHandler(IOptions<LedgerEsConfiguration> cfg)
    {
        _cfg = cfg;
    }

    public async Task<GetLedgerListResponse> Handle(GetLedgerList request, CancellationToken cancellationToken)
    {
        using var conn = new SqlConnection(_cfg.Value.SqlServerConnectionString);

        var page = request.Page ?? 0;
        var pageSize = request.PageSize ?? 10;
        var offset = page * pageSize;

        var results = await conn.QueryAsync<LedgerListView>(QUERY, new { offset, pageSize });

        return new GetLedgerListResponse(results, page, pageSize, results.Count());
    }
}

public sealed record GetLedgerListResponse(
    IEnumerable<LedgerListView> Results,
    int Page,
    int PageSize,
    int Count
);

public sealed record LedgerListView(
    Guid LedgerId,
    string LedgerName,
    bool IsOpen,
    decimal Balance,
    DateTimeOffset ModifiedDate
);
