using AutoMapper;

using EventStore.Client;

using FluentValidation;
using FluentValidation.Results;

using Js.LedgerEs.EventSourcing;

using MediatR;

namespace Js.LedgerEs;

public interface ICommand
{
    Guid GetStreamUniqueIdentifier();
}

public abstract class AbstractCommandHandler<TRequest, TResponse, TAggregate> :
    IRequestHandler<TRequest, TResponse>
        where TRequest : class, ICommand, IRequest<TResponse>
        where TResponse : class, ISerializableEvent
        where TAggregate : class, IWriteModel, new()
{
    protected IMapper Mapper { get; private set; }
    protected IEventClient EventClient { get; private set; }

    public AbstractCommandHandler(IMapper mapper, IEventClient eventClient)
    {
        Mapper = mapper;
        EventClient = eventClient;
    }

    public virtual async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken)
    {
        var streamId = request.GetStreamUniqueIdentifier();
        var streamName = EventClient.GetStreamNameForAggregate<TAggregate>(streamId);
        var aggregate = await EventClient.AggregateStream<TAggregate>(streamName, cancellationToken) ?? new TAggregate();
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

        await EventClient.AppendToStreamAsync(
            streamName,
            aggregate,
            beforeVersion == 0
                ? StreamRevision.None
                : beforeVersion - 1,
            @event,
            cancellationToken
        );

        return @event;
    }

    protected virtual TResponse MapRequestToEvent(TRequest request)
    {
        var @event = Mapper.Map<TRequest, TResponse>(request);

        @event.EventId = Guid.NewGuid();

        return @event;
    }
}
