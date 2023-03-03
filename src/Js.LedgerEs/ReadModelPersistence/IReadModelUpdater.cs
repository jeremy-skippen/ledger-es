using System.Data;

using Js.LedgerEs.EventSourcing;

using Microsoft.Data.SqlClient;

namespace Js.LedgerEs.ReadModelPersistence;

public interface IReadModelUpdater
{
    Task ApplyEventToReadModel(SqlConnection conn, IDbTransaction transaction, ISerializableEvent @event, CancellationToken cancellationToken);
}

public interface IReadModelUpdaterEventHandlerRegistration
{
    Type ReadModelUpdaterType { get; }
    Type EventType { get; }
}

public sealed class ReadModelUpdaterEventHandlerRegistration<TReadModelUpdater, TEvent>
    : IReadModelUpdaterEventHandlerRegistration
        where TReadModelUpdater : class, IReadModelUpdater
        where TEvent : class, ISerializableEvent
{
    public Type ReadModelUpdaterType => typeof(TReadModelUpdater);

    public Type EventType => typeof(TEvent);
}
