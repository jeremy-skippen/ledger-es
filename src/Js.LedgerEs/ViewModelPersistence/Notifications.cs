using Js.LedgerEs.EventSourcing;

using MediatR;

namespace Js.LedgerEs.ViewModelPersistence;

/// <summary>
/// A notification emitted when a new event is serialized to the event store.
/// </summary>
/// <param name="Event">
/// The event that was serialized.
/// </param>
public sealed record EventSerialized(ISerializableEvent Event) : INotification;

/// <summary>
/// A notification emitted when a view model is updated and written to the read database.
/// </summary>
/// <typeparam name="T">
/// The type of the view model.
/// </typeparam>
/// <param name="Model">
/// The view model that was updated.
/// </param>
public sealed record ViewModelUpdated<T>(T Model) : INotification where T : class, IViewModel;
