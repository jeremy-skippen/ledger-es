using System.Data;

using Dapper;

using Js.LedgerEs.EventSourcing;
using Js.LedgerEs.Requests;

using MediatR;

using Microsoft.Data.SqlClient;

namespace Js.LedgerEs.ReadModelPersistence;

public class DashboardReadModelUpdater : IReadModelUpdater
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

    private readonly ILogger<DashboardReadModelUpdater> _logger;
    private readonly IMediator _mediator;

    public DashboardReadModelUpdater(
        ILogger<DashboardReadModelUpdater> logger,
        IMediator mediator
    )
    {
        _logger = logger;
        _mediator = mediator;
    }

    public async Task<IAggregate?> ApplyEventToReadModel(SqlConnection conn, IDbTransaction transaction, ISerializableEvent @event, CancellationToken cancellationToken)
    {
        var dashboard = await _mediator.Send(new GetDashboard(), cancellationToken);

        dashboard.Apply(@event);

        try
        {
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
                },
                transaction
            );
            if (rowsAffected != 1)
                _logger.LogWarning("{RowsUpdated} rows affected writing read model, expected 1", rowsAffected);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write read model - read model will be out of date: {Message}", ex.Message);
            return null;
        }

        return dashboard;
    }
}
