using Dapper;

using Js.LedgerEs.Configuration;
using Js.LedgerEs.ReadModelPersistence;

using MediatR;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Js.LedgerEs.Dashboard;

public sealed class UpdateDashboardReadModelHandler : INotificationHandler<UpdateReadModel>
{
    private const string QUERY = @"
        UPDATE TOP (1) dbo.DashboardView WITH (UPDLOCK, SERIALIZABLE)
        SET LedgerCount = @LedgerCount,
            LedgerOpenCount = @LedgerOpenCount,
            LedgerClosedCount = @LedgerClosedCount,
            TransactionCount = @TransactionCount,
            ReceiptCount = @ReceiptCount,
            PaymentCount = @PaymentCount,
            NetAmount = @NetAmount,
            ReceiptAmount = @ReceiptAmount,
            PaymentAmount = @PaymentAmount,
            Version = @Version,
            ModifiedDate = @ModifiedDate
        WHERE [Version] = @Version - 1;

        IF (@@ROWCOUNT = 0)
        BEGIN
            INSERT INTO dbo.DashboardView(
                LedgerCount, LedgerOpenCount, LedgerClosedCount,
                TransactionCount, ReceiptCount, PaymentCount,
                NetAmount, ReceiptAmount, PaymentAmount,
                [Version], ModifiedDate
            )
            VALUES (
                @LedgerCount, @LedgerOpenCount, @LedgerClosedCount,
                @TransactionCount, @ReceiptCount, @PaymentCount,
                @NetAmount, @ReceiptAmount, @PaymentAmount,
                @Version, @ModifiedDate
            );
        END
    ";

    private readonly IOptions<LedgerEsConfiguration> _cfg;
    private readonly ILogger<UpdateDashboardReadModelHandler> _logger;
    private readonly IMediator _mediator;

    public UpdateDashboardReadModelHandler(
        IOptions<LedgerEsConfiguration> cfg,
        ILogger<UpdateDashboardReadModelHandler> logger,
        IMediator mediator
    )
    {
        _cfg = cfg;
        _logger = logger;
        _mediator = mediator;
    }

    public async Task Handle(UpdateReadModel request, CancellationToken cancellationToken)
    {
        var dashboard = await _mediator.Send(new GetDashboard(), cancellationToken);
        var beforeVersion = dashboard.Version;

        dashboard.Apply(request.Event);

        if (beforeVersion == dashboard.Version)
            return;

        try
        {
            using var conn = new SqlConnection(_cfg.Value.SqlServerConnectionString);
            var rowsAffected = await conn.ExecuteAsync(
                QUERY,
                new
                {
                    dashboard.LedgerCount,
                    dashboard.LedgerOpenCount,
                    dashboard.LedgerClosedCount,
                    dashboard.TransactionCount,
                    dashboard.ReceiptCount,
                    dashboard.PaymentCount,
                    dashboard.NetAmount,
                    dashboard.ReceiptAmount,
                    dashboard.PaymentAmount,
                    Version = (long)dashboard.Version,
                    dashboard.ModifiedDate,
                }
            );
            if (rowsAffected != 1)
                _logger.LogWarning("{RowsUpdated} rows affected writing read model, expected 1", rowsAffected);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write read model - read model will be out of date: {Message}", ex.Message);
            return;
        }

        await _mediator.Publish(new NotifyReadModelUpdated<DashboardReadModel>(dashboard), cancellationToken);
    }
}
