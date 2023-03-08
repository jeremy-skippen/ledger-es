using Js.LedgerEs.ReadModelPersistence;

using MediatR;

using Microsoft.AspNetCore.SignalR;

namespace Js.LedgerEs.Ledgers;

public interface ILedgerNotificationClient
{
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
}

public sealed class LedgerUpdatedNotificationHandler : INotificationHandler<NotifyReadModelUpdated<LedgerReadModel>>
{
    private readonly IHubContext<LedgerNotificationHub, ILedgerNotificationClient> _hubContext;

    public LedgerUpdatedNotificationHandler(IHubContext<LedgerNotificationHub, ILedgerNotificationClient> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task Handle(NotifyReadModelUpdated<LedgerReadModel> notification, CancellationToken cancellationToken)
        => _hubContext.Clients.Groups(notification.Model.LedgerId.ToString("d")).LedgerUpdated(notification.Model);
}
