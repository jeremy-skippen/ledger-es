using System.Text.Json;
using System.Text.Json.Serialization;

namespace Js.LedgerEs.Configuration;

/// <summary>
/// Contains the system-wide JSON configuration.
/// </summary>
internal static class JsonConfig
{
    /// <summary>
    /// The default JSON serializer options to use with System.Text.Json.
    /// </summary>
    public static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
        },
    };
}
