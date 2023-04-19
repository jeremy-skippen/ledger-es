namespace Js.LedgerEs.ViewModelPersistence;

/// <summary>
/// Contains methods used to register the hosted service that handles view model persistence.
/// </summary>
public static class ViewModelPersistenceDependencyInjection
{
    /// <summary>
    /// Add the hosted service and register related services required for view model persistence.
    /// </summary>
    /// <param name="services">
    /// The service collection.
    /// </param>
    /// <returns>
    /// The service collection, for chaining.
    /// </returns>
    public static IServiceCollection AddViewModelPersistence(this IServiceCollection services)
        => services
            .AddTransient<IProjectionRevisionRepository, ProjectionRevisionRepository>()
            .AddTransient<ISubscriptionHandler, SubscriptionHandler>()
            .AddHostedService<SubscriptionHandlerHostedService>();
}
