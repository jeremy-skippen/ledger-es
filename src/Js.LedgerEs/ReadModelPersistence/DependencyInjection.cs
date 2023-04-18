namespace Js.LedgerEs.ReadModelPersistence;

/// <summary>
/// Contains methods used to register the hosted service that handles read model persistence.
/// </summary>
public static class ReadModelPersistenceDependencyInjection
{
    /// <summary>
    /// Add the hosted service and register related services required for read model persistence.
    /// </summary>
    /// <param name="services">
    /// The service collection.
    /// </param>
    /// <returns>
    /// The service collection, for chaining.
    /// </returns>
    public static IServiceCollection AddReadModelPersistence(this IServiceCollection services)
        => services
            .AddTransient<IProjectionRevisionRepository, ProjectionRevisionRepository>()
            .AddTransient<ISubscriptionHandler, SubscriptionHandler>()
            .AddHostedService<SubscriptionHandlerHostedService>();
}
