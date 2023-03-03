namespace Js.LedgerEs.ReadModelPersistence;

public static class ReadModelPersistenceDependencyInjection
{
    public static IServiceCollection AddReadModelPersistence(this IServiceCollection services)
        => services
            .AddTransient<IProjectionRevisionRepository, ProjectionRevisionRepository>()
            .AddTransient<LedgerReadModelUpdater>()
            .AddTransient<SubscriptionHandler>()
            .AddHostedService<SubscriptionHandlerHostedService>();
}
