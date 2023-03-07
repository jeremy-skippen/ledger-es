using System.Reflection;

namespace Js.LedgerEs.EventSourcing;

public static class EventSourcingDependencyExtensions
{
    public static IServiceCollection AddEventSerialization(this IServiceCollection services, params Assembly[] assemblies)
    {
        var types = assemblies.SelectMany(
            a => a
                .GetTypes()
                .Where(t => t.IsAssignableTo(typeof(ISerializableEvent)))
                .Where(t => t.IsClass && !t.IsInterface && !t.IsAbstract)
        );

        foreach (var type in types)
            services.AddSingleton(new SerializableEventRegistration(type.Name, type));

        services.AddSingleton<IEventClient, EventClient>();

        return services;
    }
}
