using Js.LedgerEs.EventSourcing;

using MediatR;

namespace Js.LedgerEs.ReadModelPersistence;

/// <summary>
/// A notification emitted when a new event is serialized to the event store.
/// </summary>
/// <param name="Event">
/// The event that was serialized.
/// </param>
public sealed record EventSerialized(ISerializableEvent Event) : INotification;

/// <summary>
/// A notification emitted when a read model is updated and written to the read database.
/// </summary>
/// <typeparam name="T">
/// The type of the read model.
/// </typeparam>
/// <param name="Model">
/// The read model that was updated.
/// </param>
public sealed record ReadModelUpdated<T>(T Model) : INotification where T : class, IReadModel;
