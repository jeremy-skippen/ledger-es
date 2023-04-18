using Js.LedgerEs.ReadModelPersistence;

using MediatR;

using Microsoft.AspNetCore.SignalR;

namespace Js.LedgerEs.Ledgers;

public interface ILedgerNotificationClient
{
    Task LedgerAdded(LedgerReadModel dashboard);
    Task LedgerUpdated(LedgerReadModel dashboard);
}

public sealed class LedgerNotificationHub : Hub<ILedgerNotificationClient>
{
    public override async Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();
        if (httpContext != null && httpContext.Request.Query.TryGetValue("ledgerId", out var ledgerIdValues))
        {
            foreach (var ledgerId in ledgerIdValues.Where(l => !string.IsNullOrEmpty(l)))
                await Groups.AddToGroupAsync(Context.ConnectionId, ledgerId!);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var httpContext = Context.GetHttpContext();
        if (httpContext != null && httpContext.Request.Query.TryGetValue("ledgerId", out var ledgerIdValues))
        {
            foreach (var ledgerId in ledgerIdValues.Where(l => !string.IsNullOrEmpty(l)))
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, ledgerId!);
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task ListenToLedgers(string[] ledgerIds)
    {
        foreach (var ledgerId in ledgerIds)
            await Groups.AddToGroupAsync(Context.ConnectionId, ledgerId);
    }

    public async Task UnListenToLedgers(string[] ledgerIds)
    {
        foreach (var ledgerId in ledgerIds)
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, ledgerId);
    }
}

public sealed class LedgerUpdatedNotificationHandler : INotificationHandler<ReadModelUpdated<LedgerReadModel>>
{
    private readonly IHubContext<LedgerNotificationHub, ILedgerNotificationClient> _hubContext;

    public LedgerUpdatedNotificationHandler(IHubContext<LedgerNotificationHub, ILedgerNotificationClient> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task Handle(ReadModelUpdated<LedgerReadModel> notification, CancellationToken cancellationToken)
    {
        var model = notification.Model;
        if (model.Version == 1)
            await _hubContext.Clients
                .All
                .LedgerAdded(model);
        else
            await _hubContext.Clients
                .Groups(model.LedgerId.ToString("d"))
                .LedgerUpdated(model);
    }
}
