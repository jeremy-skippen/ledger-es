using MediatR;

namespace Js.LedgerEs.Dashboard;

public static class DashboardDependencyInjection
{
    public static void MapDashboardRoutes(this IEndpointRouteBuilder api)
    {
        api.MapGet(
            "/dashboard",
            async (IMediator mediator, CancellationToken ct)
                => await mediator.Send(new GetDashboard(), ct)
        );
    }

    public static void MapDashboardHubs(this IEndpointRouteBuilder api)
    {
        api.MapHub<DashboardNotificationHub>("/dashboard");
    }
}
