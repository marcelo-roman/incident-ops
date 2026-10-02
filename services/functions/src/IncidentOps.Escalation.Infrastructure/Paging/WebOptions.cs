using System.ComponentModel.DataAnnotations;

namespace IncidentOps.Escalation.Infrastructure.Paging;

public sealed class WebOptions
{
    public const string Section = "Web";

    [Required]
    public Uri? BaseUrl { get; set; }
}
