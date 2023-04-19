using Js.LedgerEs.EventSourcing;

namespace Js.LedgerEs.ViewModelPersistence;

/// <summary>
/// Represents an aggregate used for data presentation.
/// Read models can be serialized to a secondary data store to populate materialized views.
/// </summary>
public interface IViewModel : IAggregate
{
}
