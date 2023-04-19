using MediatR;

namespace Js.LedgerEs.Dashboard;

public static class DashboardRoutes
{
    public static IEndpointRouteBuilder MapDashboardRoutes(this IEndpointRouteBuilder api)
    {
        api.MapGet(
            "/dashboard",
            async (IMediator mediator, CancellationToken ct)
                => await mediator.Send(new GetDashboard(), ct)
        );

        return api;
    }

    public static IEndpointRouteBuilder MapDashboardHubs(this IEndpointRouteBuilder signalr)
    {
        signalr.MapHub<DashboardNotificationHub>("/dashboard");

        return signalr;
    }
}
