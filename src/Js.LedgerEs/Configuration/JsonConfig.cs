using System.Text.Json;
using System.Text.Json.Serialization;

namespace Js.LedgerEs.Configuration;

public static class JsonConfig
{
    public static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        },
    };
}
