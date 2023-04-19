using Dapper;

using Js.LedgerEs.Configuration;

using MediatR;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Js.LedgerEs.Dashboard;

public sealed record GetDashboard() : IRequest<DashboardViewModel>;

public sealed class GetDashboardRequestHandler : IRequestHandler<GetDashboard, DashboardViewModel>
{
    private readonly IOptions<LedgerEsConfiguration> _cfg;

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

    public GetDashboardRequestHandler(IOptions<LedgerEsConfiguration> cfg)
    {
        _cfg = cfg;
    }

    public async Task<DashboardViewModel> Handle(GetDashboard request, CancellationToken cancellationToken)
    {
        using var conn = new SqlConnection(_cfg.Value.SqlServerConnectionString);

        return await conn.QuerySingleOrDefaultAsync<DashboardViewModel>(QUERY) ?? new DashboardViewModel();
    }
}
