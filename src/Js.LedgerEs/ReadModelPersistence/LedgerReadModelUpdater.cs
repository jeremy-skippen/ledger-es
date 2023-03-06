using System.Data;
using System.Text;
using System.Text.Json;

using Dapper;

using Js.LedgerEs.Configuration;
using Js.LedgerEs.EventSourcing;
using Js.LedgerEs.Requests;

using MediatR;

using Microsoft.Data.SqlClient;

namespace Js.LedgerEs.ReadModelPersistence;

public class LedgerReadModelUpdater : IReadModelUpdater
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

    private readonly ILogger<LedgerReadModelUpdater> _logger;
    private readonly IMediator _mediator;

    public LedgerReadModelUpdater(
        ILogger<LedgerReadModelUpdater> logger,
        IMediator mediator
    )
    {
        _logger = logger;
        _mediator = mediator;
    }

    public async Task ApplyEventToReadModel(SqlConnection conn, IDbTransaction transaction, ISerializableEvent @event, CancellationToken cancellationToken)
    {
        var ledgerId = @event.GetStreamUniqueIdentifier();
        var ledgerResponse = await _mediator.Send(new GetLedger(ledgerId), cancellationToken);
        var ledger = ledgerResponse.Ledger ?? new Ledger();

        try
        {
            ledger.Apply(@event);
        }
        catch (InvalidStateTransitionException ex)
        {
            _logger.LogError(ex, "Failed to apply event to existing read model - read model will be out of date: {Message}", ex.Message);
        }

        try
        {
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
                },
                transaction
            );
            if (rowsAffected != 1)
                _logger.LogWarning("{RowsUpdated} rows affected writing read model, expected 1", rowsAffected);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write read model - read model will be out of date: {Message}", ex.Message);
        }
    }
}
