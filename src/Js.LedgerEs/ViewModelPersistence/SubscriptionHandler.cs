using System.Transactions;

using Js.LedgerEs.EventSourcing;

using MediatR;

namespace Js.LedgerEs.ViewModelPersistence;

/// <summary>
/// Handles a persistent subscription to the event store "all" stream and emits notifications to the MediatR bus to
/// enable view models to be constructed and persisted to a read database.
/// </summary>
public interface ISubscriptionHandler
{
    /// <summary>
    /// Subscribe to the all stream of the event store.
    /// </summary>
    /// <param name="ct">
    /// Cancellation token.
    /// </param>
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

        await _eventClient.SubscribeToAllStreamAsync(
            position,
            onEventAppeared: HandleEvent,
            onSubscriptionDropped: HandleDrop,
            cancellationToken: ct
        );

        _logger.LogInformation("Subscription to $all stream started");
    }

    private async Task HandleEvent(ISerializableEvent? @event, ulong position, CancellationToken ct)
    {
        try
        {
            using var transaction = new TransactionScope(TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Enabled);

            if (@event is not null)
            {
                _logger.LogInformation("Persisting changes to view model from event {Event}", @event);

                await _mediator.Publish(new EventSerialized(@event), ct);
            }

            await _revisionRepository.SetStreamPosition(PROJECTION_NAME, position);

            transaction.Complete();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consuming event {Event} - view model will be out of date", @event);
        }
    }

    private void HandleDrop(string reason, Exception? exception)
    {
        _logger.LogError(
            exception,
            "Subscription to $all dropped because {Reason}",
            reason
        );
    }
}

/// <summary>
/// Hosted service that handles the subscription to the event store "all" stream.
/// </summary>
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
