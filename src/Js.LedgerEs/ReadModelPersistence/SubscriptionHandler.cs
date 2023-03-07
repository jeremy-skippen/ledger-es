using EventStore.Client;

using Js.LedgerEs.Configuration;
using Js.LedgerEs.EventSourcing;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Js.LedgerEs.ReadModelPersistence;

public interface ISubscriptionHandler
{
    Task SubscribeToAll(CancellationToken ct);
}

public class SubscriptionHandler : ISubscriptionHandler
{
    private readonly IOptions<LedgerEsConfiguration> _cfg;
    private readonly ILogger<SubscriptionHandler> _logger;
    private readonly EventStoreClient _eventStore;
    private readonly IProjectionRevisionRepository _revisionRepository;
    private readonly IDictionary<Type, IReadModelUpdater[]> _eventTypeUpdaterMap;

    private const string PROJECTION_NAME = "default";

    public SubscriptionHandler(
        IOptions<LedgerEsConfiguration> cfg,
        ILogger<SubscriptionHandler> logger,
        EventStoreClient eventStore,
        IProjectionRevisionRepository revisionRepository,
        IEnumerable<IReadModelUpdater> updaters,
        IEnumerable<IReadModelUpdaterEventHandlerRegistration> updaterEventRegistrations
    )
    {
        _cfg = cfg;
        _logger = logger;
        _eventStore = eventStore;
        _revisionRepository = revisionRepository;
        _eventTypeUpdaterMap = updaterEventRegistrations
            .Select(r => new
            {
                r.EventType,
                ReadModelUpdater = updaters.Single(u => u.GetType() == r.ReadModelUpdaterType),
            })
            .GroupBy(r => r.EventType)
            .ToDictionary(k => k.Key, v => v.Select(r => r.ReadModelUpdater).ToArray());
    }

    private IReadModelUpdater[] GetUpdatersForEvent(ISerializableEvent @event)
        => _eventTypeUpdaterMap.TryGetValue(@event.GetType(), out var updaters)
            ? updaters
            : Array.Empty<IReadModelUpdater>();

    public async Task SubscribeToAll(CancellationToken ct)
    {
        _logger.LogInformation("Subscribing to $all stream");

        using var conn = new SqlConnection(_cfg.Value.SqlServerConnectionString);

        var position = await _revisionRepository.GetStreamPosition(conn, PROJECTION_NAME);

        await _eventStore.SubscribeToAllAsync(
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
            using var conn = new SqlConnection(_cfg.Value.SqlServerConnectionString);
            await conn.OpenAsync(ct);

            using var transaction = conn.BeginTransaction();

            try
            {
                var @event = resolvedEvent.DeserializeFromResolvedEvent();
                if (@event is not null)
                {
                    _logger.LogInformation("Persisting changes to read model from event {Event}", @event);

                    var updaters = GetUpdatersForEvent(@event);
                    foreach (var updater in updaters)
                    {
                        var readModel = await updater.ApplyEventToReadModel(conn, transaction, @event, ct);
                    }
                }

                await _revisionRepository.SetStreamPosition(conn, transaction, PROJECTION_NAME, resolvedEvent.Event.Position);
                await transaction.CommitAsync(ct);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync(ct);

                throw;
            }
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
