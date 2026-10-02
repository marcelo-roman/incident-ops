using System.ComponentModel.DataAnnotations;

namespace IncidentOps.Escalation.Infrastructure.IncidentsApi;

public sealed class IncidentsApiOptions
{
    public const string Section = "IncidentsApi";

    [Required]
    public Uri? BaseUrl { get; set; }

    [Required]
    public string ApiKey { get; set; } = string.Empty;
}
