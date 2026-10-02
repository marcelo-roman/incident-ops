using System.ComponentModel.DataAnnotations;

namespace IncidentOps.Escalation.Infrastructure.Paging;

public sealed class NotificationOptions
{
    public const string Section = "Notifications";

    [Required]
    public Uri? LogicAppUrl { get; set; }
}
