namespace IncidentOps.Api.Security;

public sealed class ApiKeyOptions
{
    public const string SectionName = "Security";

    public string? EscalationApiKey { get; set; }
}
