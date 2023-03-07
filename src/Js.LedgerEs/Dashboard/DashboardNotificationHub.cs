using Js.LedgerEs.ReadModelPersistence;

using MediatR;

using Microsoft.AspNetCore.SignalR;

namespace Js.LedgerEs.Dashboard;

public interface IDashboardNotificationClient
{
    Task DashboardUpdated(DashboardReadModel dashboard);
}

public sealed class DashboardNotificationHub : Hub<IDashboardNotificationClient>
{
}

public sealed class DashboardUpdatedNotificationHandler : INotificationHandler<NotifyReadModelUpdated<DashboardReadModel>>
{
    private readonly IHubContext<DashboardNotificationHub, IDashboardNotificationClient> _hubContext;

    public DashboardUpdatedNotificationHandler(IHubContext<DashboardNotificationHub, IDashboardNotificationClient> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task Handle(NotifyReadModelUpdated<DashboardReadModel> notification, CancellationToken cancellationToken)
        => _hubContext.Clients.All.DashboardUpdated(notification.Model);
}
