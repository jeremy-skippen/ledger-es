using System.Text.Json;
using System.Text.Json.Serialization;

namespace Js.LedgerEs;

public static class JsonConfig
{
    public static readonly JsonSerializerOptions SerializerOptions = new()
    {
        Converters = {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        },
    };
}
