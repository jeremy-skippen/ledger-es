using System.Transactions;

using EventStore.Client;

using Js.LedgerEs.EventSourcing;

using MediatR;

namespace Js.LedgerEs.ReadModelPersistence;

public interface ISubscriptionHandler
{
    Task SubscribeToAll(CancellationToken ct);
}

public class SubscriptionHandler : ISubscriptionHandler
{
    private readonly ILogger<SubscriptionHandler> _logger;
    private readonly IMediator _mediator;
    private readonly IEventClient _eventClient;
    private readonly IProjectionRevisionRepository _revisionRepository;

    private const string PROJECTION_NAME = "default";

    public SubscriptionHandler(
        ILogger<SubscriptionHandler> logger,
        IMediator mediator,
        IEventClient eventClient,
        IProjectionRevisionRepository revisionRepository
    )
    {
        _logger = logger;
        _mediator = mediator;
        _eventClient = eventClient;
        _revisionRepository = revisionRepository;
    }

    public async Task SubscribeToAll(CancellationToken ct)
    {
        _logger.LogInformation("Subscribing to $all stream");

        var position = await _revisionRepository.GetStreamPosition(PROJECTION_NAME);

        await _eventClient.SubscribeToAllAsync(
            position.HasValue ? FromAll.After(position.Value) : FromAll.Start,
            eventAppeared: HandleEvent,
            subscriptionDropped: HandleDrop,
            cancellationToken: ct
        );

        _logger.LogInformation("Subscription to $all stream started");
    }

    private async Task HandleEvent(StreamSubscription _, ResolvedEvent resolvedEvent, CancellationToken ct)
    {
        try
        {
            using var transaction = new TransactionScope(TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Enabled);

            var @event = _eventClient.DeserializeFromResolvedEvent(resolvedEvent);
            if (@event is not null)
            {
                _logger.LogInformation("Persisting changes to read model from event {Event}", @event);

                await _mediator.Publish(new UpdateReadModel(@event), ct);
            }

            await _revisionRepository.SetStreamPosition(PROJECTION_NAME, resolvedEvent.Event.Position);

            transaction.Complete();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consuming event {ResolvedEvent} - read model will be out of date", resolvedEvent);
        }
    }

    private void HandleDrop(StreamSubscription _, SubscriptionDroppedReason reason, Exception? exception)
    {
        _logger.LogError(
            exception,
            "Subscription to $all dropped with '{Reason}'",
            reason
        );
    }
}

public class SubscriptionHandlerHostedService : IHostedService
{
    private readonly ISubscriptionHandler _handler;

    private Task? _handlerTask;
    private CancellationTokenSource? _handlerCancellationTokenSource;

    public SubscriptionHandlerHostedService(ISubscriptionHandler handler)
    {
        _handler = handler;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _handlerCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _handlerTask = Task.Run(() => _handler.SubscribeToAll(_handlerCancellationTokenSource.Token), cancellationToken);

        return _handlerTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_handlerTask == null)
            return;

        _handlerCancellationTokenSource?.Cancel();

        await Task.WhenAny(_handlerTask, Task.Delay(-1, cancellationToken));

        cancellationToken.ThrowIfCancellationRequested();
    }
}
