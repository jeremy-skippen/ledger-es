using Dapper;

using Js.LedgerEs.Configuration;

using MediatR;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Js.LedgerEs.Requests;

public sealed record GetDashboard() : IRequest<Dashboard>;

public sealed class GetDashboardRequestHandler : IRequestHandler<GetDashboard, Dashboard>
{
    private readonly IOptions<LedgerEsConfiguration> _cfg;
    private readonly IMediator _mediator;

    private const string QUERY = @"
        SELECT TOP (1)
            LedgerCount,
            LedgerOpenCount,
            LedgerClosedCount,
            TransactionCount,
            ReceiptCount,
            PaymentCount,
            NetAmount,
            ReceiptAmount,
            PaymentAmount,
            [Version],
            ModifiedDate
        FROM dbo.DashboardView;
    ";

    public GetDashboardRequestHandler(IOptions<LedgerEsConfiguration> cfg, IMediator mediator)
    {
        _cfg = cfg;
        _mediator = mediator;
    }

    public async Task<Dashboard> Handle(GetDashboard request, CancellationToken cancellationToken)
    {
        using var conn = new SqlConnection(_cfg.Value.SqlServerConnectionString);

        return await conn.QuerySingleOrDefaultAsync<Dashboard>(QUERY) ?? new Dashboard();
    }
}
