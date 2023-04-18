using System.Reflection;

using EventStore.Client;

namespace Js.LedgerEs.EventSourcing;

/// <summary>
/// Contains methods used to register the event store client and required metadata.
/// </summary>
public static class EventSourcingDependencyExtensions
{
    /// <summary>
    /// Add the event store client and register supported events for use with event sourcing.
    /// </summary>
    /// <param name="services">
    /// The service collection.
    /// </param>
    /// <param name="configuration">
    /// The application configuration, used to retrieve the event store connection string.
    /// </param>
    /// <param name="assemblies">
    /// The list of assemblies to scan. Event objects implementing <see cref="ISerializableEvent"/> in these assemblies
    /// will be registered in the client and be able to be serialized to the event store.
    /// </param>
    /// <returns>
    /// The service collection, for chaining.
    /// </returns>
    public static IServiceCollection AddEventSourcing(this IServiceCollection services, IConfiguration configuration, params Assembly[] assemblies)
    {
        var types = assemblies.SelectMany(
            a => a
                .GetTypes()
                .Where(t => t.IsAssignableTo(typeof(ISerializableEvent)))
                .Where(t => t.IsClass && !t.IsInterface && !t.IsAbstract)
        );

        foreach (var type in types)
            services.AddSingleton(new SerializableEventRegistration(type.Name, type));

        services
            .AddSingleton(new EventStoreClient(EventStoreClientSettings.Create(configuration.GetConnectionString("EventStore"))))
            .AddSingleton<IEventClient, EventClient>();

        return services;
    }
}
