using EventStore.Client;

using Js.LedgerEs.Configuration;
using Js.LedgerEs.EventSourcing;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Js.LedgerEs.ReadModelPersistence;

public class SubscriptionHandler
{
    private readonly IOptions<LedgerEsConfiguration> _cfg;
    private readonly ILogger<SubscriptionHandler> _logger;
    private readonly EventStoreClient _eventStore;
    private readonly IProjectionRevisionRepository _revisionRepository;
    private readonly IServiceProvider _services;
    private readonly IDictionary<Type, IList<Type>> _eventTypeMap;
    private readonly IDictionary<Type, Type> _aggregateUpdaterMap;

    private const string PROJECTION_NAME = "default";

    public SubscriptionHandler(
        IOptions<LedgerEsConfiguration> cfg,
        ILogger<SubscriptionHandler> logger,
        EventStoreClient eventStore,
        IProjectionRevisionRepository revisionRepository,
        IServiceProvider services
    )
    {
        _cfg = cfg;
        _logger = logger;
        _eventStore = eventStore;
        _revisionRepository = revisionRepository;
        _services = services;
        _eventTypeMap = new Dictionary<Type, IList<Type>>();
        _aggregateUpdaterMap = new Dictionary<Type, Type>();

        PopulateTypeMaps();
    }

    private void PopulateTypeMaps()
    {
        var types = typeof(SubscriptionHandler).Assembly.GetTypes();

        var aggregateTypes = types
            .Where(t => t.IsAssignableTo(typeof(IAggregate)) && t.IsAssignableTo(typeof(IEventHandler)))
            .Where(t => t.IsClass && !t.IsAbstract)
            .Select(t => new
            {
                AggregateType = t,
                EventHandlerInterfaces = t.GetInterfaces().Where(i => i.IsGenericType).Where(i => i.GetInterfaces().Contains(typeof(IEventHandler))).ToList(),
            })
            .Select(t => new
            {
                t.AggregateType,
                EventsHandled = t.EventHandlerInterfaces.Select(i => i.GetGenericArguments()[0]),
            })
            .SelectMany(t => t.EventsHandled.Select(e => new { EventType = e, t.AggregateType }))
            .GroupBy(t => t.EventType)
            .ToList();
        foreach (var group in aggregateTypes)
            _eventTypeMap[group.Key] = group.Select(t => t.AggregateType).ToList();

        var updaterTypes = types
            .Where(t => t.IsAssignableTo(typeof(IReadModelUpdater)))
            .Where(t => t.IsClass && !t.IsAbstract)
            .Select(t => new
            {
                UpdaterType = t,
                UpdaterInterfaces = t.GetInterfaces().Where(i => i.IsGenericType).Where(i => i.GetInterfaces().Contains(typeof(IReadModelUpdater))).ToList(),
            })
            .Select(t => new
            {
                t.UpdaterType,
                AggregateHandled = t.UpdaterInterfaces.Select(i => i.GetGenericArguments()[0]).Single(),
            })
            .ToList();
        foreach (var t in updaterTypes)
            _aggregateUpdaterMap[t.AggregateHandled] = t.UpdaterType;
    }

    private IReadModelUpdater[] GetUpdatersForEvent(ISerializableEvent @event)
    {
        var aggregateTypes = _eventTypeMap.TryGetValue(@event.GetType(), out var aggregateTypeList) ? aggregateTypeList.ToArray() : Array.Empty<Type>();
        var updaterTypes = aggregateTypes.Select(t => _aggregateUpdaterMap.TryGetValue(t, out var updaterType) ? updaterType : null).Where(t => t is not null);

        return updaterTypes
            .Select(t => _services.GetRequiredService(t!) as IReadModelUpdater)
            .Where(s => s is not null)
            .Select(s => s!)
            .ToArray();
    }

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
                        await updater.ApplyEventToReadModel(conn, transaction, @event, ct);
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
    private readonly SubscriptionHandler _handler;

    private Task? _handlerTask;
    private CancellationTokenSource? _handlerCancellationTokenSource;

    public SubscriptionHandlerHostedService(SubscriptionHandler handler)
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
