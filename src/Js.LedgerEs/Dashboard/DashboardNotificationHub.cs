using Js.LedgerEs.ViewModelPersistence;

using MediatR;

using Microsoft.AspNetCore.SignalR;

namespace Js.LedgerEs.Dashboard;

public interface IDashboardNotificationClient
{
    Task DashboardUpdated(DashboardViewModel dashboard);
}

public sealed class DashboardNotificationHub : Hub<IDashboardNotificationClient>
{
}

public sealed class DashboardUpdatedNotificationHandler : INotificationHandler<ViewModelUpdated<DashboardViewModel>>
{
    private readonly IHubContext<DashboardNotificationHub, IDashboardNotificationClient> _hubContext;

    public DashboardUpdatedNotificationHandler(IHubContext<DashboardNotificationHub, IDashboardNotificationClient> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task Handle(ViewModelUpdated<DashboardViewModel> notification, CancellationToken cancellationToken)
        => _hubContext.Clients.All.DashboardUpdated(notification.Model);
}
