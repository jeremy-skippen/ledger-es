using System.Data;

using Js.LedgerEs.EventSourcing;

using Microsoft.Data.SqlClient;

namespace Js.LedgerEs.ReadModelPersistence;

public interface IReadModelUpdater
{
    Task ApplyEventToReadModel(SqlConnection conn, IDbTransaction transaction, ISerializableEvent @event, CancellationToken cancellationToken);
}

public interface IReadModelUpdater<TAggregate> : IReadModelUpdater where TAggregate : IAggregate
{
}
