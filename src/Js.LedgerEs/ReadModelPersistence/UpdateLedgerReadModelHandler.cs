using System.Text;
using System.Text.Json;

using Dapper;

using Js.LedgerEs.Configuration;
using Js.LedgerEs.Requests;

using MediatR;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Js.LedgerEs.ReadModelPersistence;

public class UpdateLedgerReadModelHandler : INotificationHandler<UpdateReadModel>
{
    private const string INSERT_QUERY = @"
        INSERT INTO dbo.LedgerView(LedgerId, LedgerName, IsOpen, Entries, Balance, [Version], ModifiedDate)
        VALUES (@LedgerId, @LedgerName, @IsOpen, @Entries, @Balance, @Version, @ModifiedDate);
    ";
    private const string UPDATE_QUERY = @"
        UPDATE dbo.LedgerView
        SET LedgerName = @LedgerName,
            IsOpen = @IsOpen,
            Entries = @Entries,
            Balance = @Balance,
            [Version] = @Version,
            ModifiedDate = @ModifiedDate
        WHERE LedgerId = @LedgerId
        AND [Version] = @Version - 1;
    ";

    private readonly IOptions<LedgerEsConfiguration> _cfg;
    private readonly ILogger<UpdateLedgerReadModelHandler> _logger;
    private readonly IMediator _mediator;

    public UpdateLedgerReadModelHandler(
        IOptions<LedgerEsConfiguration> cfg,
        ILogger<UpdateLedgerReadModelHandler> logger,
        IMediator mediator
    )
    {
        _cfg = cfg;
        _logger = logger;
        _mediator = mediator;
    }

    public async Task Handle(UpdateReadModel request, CancellationToken cancellationToken)
    {
        var ledgerId = request.Event.GetStreamUniqueIdentifier();
        var ledgerResponse = await _mediator.Send(new GetLedger(ledgerId), cancellationToken);
        var ledger = ledgerResponse.Ledger ?? new LedgerReadModel();
        var beforeVersion = ledger.Version;

        try
        {
            ledger.Apply(request.Event);
        }
        catch (InvalidStateTransitionException ex)
        {
            _logger.LogError(ex, "Failed to apply event to existing read model - read model will be out of date: {Message}", ex.Message);
            return;
        }

        if (beforeVersion == ledger.Version)
            return;

        try
        {
            using var conn = new SqlConnection(_cfg.Value.SqlServerConnectionString);
            var query = ledger.Version == 0 ? INSERT_QUERY : UPDATE_QUERY;
            var rowsAffected = await conn.ExecuteAsync(
                query,
                new
                {
                    ledger.LedgerId,
                    ledger.LedgerName,
                    ledger.IsOpen,
                    Entries = Encoding.UTF8.GetString(JsonSerializer.SerializeToUtf8Bytes(ledger.Entries, JsonConfig.SerializerOptions)),
                    ledger.Balance,
                    Version = (long)ledger.Version,
                    ledger.ModifiedDate,
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

        await _mediator.Publish(new NotifyReadModelUpdated<LedgerReadModel>(ledger), cancellationToken);
    }
}
