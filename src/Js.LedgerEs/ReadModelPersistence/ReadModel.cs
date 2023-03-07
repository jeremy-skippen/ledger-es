using Js.LedgerEs.EventSourcing;

using MediatR;

namespace Js.LedgerEs.ReadModelPersistence;

public interface IReadModel : IAggregate
{
}

public sealed record UpdateReadModel(ISerializableEvent Event) : INotification;

public sealed record NotifyReadModelUpdated<T>(T Model) : INotification where T : class, IReadModel;
