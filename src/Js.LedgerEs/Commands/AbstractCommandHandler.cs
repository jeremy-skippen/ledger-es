using AutoMapper;

using EventStore.Client;

using FluentValidation;
using FluentValidation.Results;

using Js.LedgerEs.EventSourcing;

using MediatR;

namespace Js.LedgerEs.Commands;

public interface ICommand
{
    Guid GetStreamUniqueIdentifier();
}

public abstract class AbstractCommandHandler<TRequest, TResponse, TAggregate> :
    IRequestHandler<TRequest, TResponse>
        where TRequest : class, ICommand, IRequest<TResponse>
        where TResponse : class, ISerializableEvent
        where TAggregate : class, IAggregate, new()
{
    protected IMapper Mapper { get; private set; }
    protected EventStoreClient EventStore { get; private set; }

    public AbstractCommandHandler(IMapper mapper, EventStoreClient eventStore)
    {
        Mapper = mapper;
        EventStore = eventStore;
    }

    public virtual async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken)
    {
        var streamId = request.GetStreamUniqueIdentifier();
        var streamName = streamId.GetStreamNameForAggregate<TAggregate>();
        var aggregate = await EventStore.AggregateStream<TAggregate>(streamName, cancellationToken) ?? new TAggregate();
        var beforeVersion = aggregate.Version;
        var @event = MapRequestToEvent(request);

        try
        {
            aggregate.Apply(@event);
        }
        catch (InvalidStateTransitionException ex)
        {
            throw new ValidationException(ex.Message, new[] { new ValidationFailure(ex.EventProperty ?? "", ex.Message) });
        }

        var eventData = @event.SerializeToEventData();

        try
        {
            await EventStore.AppendToStreamAsync(
                streamName,
                beforeVersion == ulong.MaxValue
                    ? StreamRevision.None
                    : beforeVersion,
                new[] { eventData },
                cancellationToken: cancellationToken
            );
        }
        catch (WrongExpectedVersionException ex)
        {
            throw new EventStoreConcurrencyException(aggregate, ex.Message);
        }

        return @event;
    }

    protected virtual TResponse MapRequestToEvent(TRequest request)
    {
        var @event = Mapper.Map<TRequest, TResponse>(request);

        @event.EventId = Guid.NewGuid();

        return @event;
    }
}
