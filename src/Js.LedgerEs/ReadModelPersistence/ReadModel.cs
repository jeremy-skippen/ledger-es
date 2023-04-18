using Js.LedgerEs.EventSourcing;

using MediatR;

namespace Js.LedgerEs.ReadModelPersistence;

/// <summary>
/// Represents an aggregate used for data presentation.
/// Read models can be serialized to a secondary data store to populate materialized views.
/// </summary>
public interface IReadModel : IAggregate
{
}
