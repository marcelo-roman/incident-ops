using System.Text.Json;
using System.Text.Json.Serialization;

namespace IncidentOps.Application.Common;

public static class ContractJson
{
    public static JsonSerializerOptions Options { get; } = Configure(new JsonSerializerOptions(JsonSerializerDefaults.Web));

    public static JsonSerializerOptions Configure(JsonSerializerOptions options)
    {
        options.Converters.Add(new JsonStringEnumConverter());
        options.Converters.Add(new UtcDateTimeOffsetConverter());
        return options;
    }
}
