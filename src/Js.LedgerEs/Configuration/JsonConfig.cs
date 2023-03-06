using System.Text.Json;
using System.Text.Json.Serialization;

namespace Js.LedgerEs.Configuration;

public static class JsonConfig
{
    public static readonly JsonSerializerOptions SerializerOptions = new()
    {
        Converters = {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        },
    };
}
