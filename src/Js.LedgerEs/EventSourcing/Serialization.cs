using System.Collections.Concurrent;
using System.Text.Json;

using EventStore.Client;

namespace Js.LedgerEs.EventSourcing;

public class SerializableEventMapper
{
    private static readonly SerializableEventMapper _self = new();

    private readonly ConcurrentDictionary<Type, string> _typeNameMap = new();
    private readonly ConcurrentDictionary<string, Type> _nameTypeMap = new();

    public static bool TryCreateMap(Type eventType)
    {
        if (eventType.IsInterface || eventType.IsAbstract)
            return false;

        return
            _self._typeNameMap.TryAdd(eventType, eventType.Name)
            && _self._nameTypeMap.TryAdd(eventType.Name, eventType);
    }

    public static string? GetName<T>() => GetName(typeof(T));

    public static string? GetName(Type eventType)
        => _self._typeNameMap.TryGetValue(eventType, out var name) ? name : null;

    public static Type? GetType(string eventName)
        => _self._nameTypeMap.TryGetValue(eventName, out var type) ? type : null;
}

public static class SerializationExtensions
{
    public static IApplicationBuilder UseEventSerialization(this IApplicationBuilder app)
    {
        var types = typeof(SerializationExtensions).Assembly
            .GetTypes()
            .Where(t => t.IsAssignableTo(typeof(ISerializableEvent)))
            .Where(t => !t.IsInterface && !t.IsAbstract)
            .ToList();

        foreach (var type in types)
            SerializableEventMapper.TryCreateMap(type);

        return app;
    }

    public static string GetStreamNameForAggregate<TAggregate>(this Guid id)
        => $"{typeof(TAggregate).Name.ToLowerInvariant()}-{id:d}";

    public static object? DeserializeFromResolvedEvent(this ResolvedEvent resolvedEvent)
    {
        var type = SerializableEventMapper.GetType(resolvedEvent.Event.EventType);

        return type != null
            ? JsonSerializer.Deserialize(resolvedEvent.Event.Data.Span, type)
            : null;
    }

    public static EventData SerializeToEventData<T>(this T @event) where T : class, ISerializableEvent
        => new(
            Uuid.FromGuid(@event.EventId),
            SerializableEventMapper.GetName(@event.GetType()) ?? "UnknownEvent",
            JsonSerializer.SerializeToUtf8Bytes(@event),
            JsonSerializer.SerializeToUtf8Bytes(new { })
        );
}
