using System.Text.Json.Serialization;
using IncidentOps.Application.Incidents;

namespace IncidentOps.Infrastructure.Messaging;

internal sealed record CloudEventEnvelope(
    [property: JsonPropertyName("specversion")] string SpecVersion,
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("source")] string Source,
    [property: JsonPropertyName("time")] DateTimeOffset Time,
    [property: JsonPropertyName("subject")] string Subject,
    [property: JsonPropertyName("datacontenttype")] string DataContentType,
    [property: JsonPropertyName("data")] IncidentView Data);
