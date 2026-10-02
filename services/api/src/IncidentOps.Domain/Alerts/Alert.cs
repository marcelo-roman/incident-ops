using IncidentOps.Domain.Common;
using IncidentOps.Domain.Incidents;

namespace IncidentOps.Domain.Alerts;

public sealed record Alert
{
    private const string UnnamedAlert = "unnamed-alert";

    private Alert(AlertSignal signal)
    {
        Guard.Against(signal.Source == IncidentSource.Manual, "source", "Alerts come from an alerting source.");
        Source = Guard.Defined(signal.Source, "source");
        Fingerprint = signal.Fingerprint;
        Status = Guard.Defined(signal.Status, "status");
        Name = Text.Truncate(Text.FirstNonBlank(signal.Name, UnnamedAlert), IncidentTitle.MaxLength);
        Title = new IncidentTitle(Text.Truncate(Text.FirstNonBlank(signal.Summary, Name), IncidentTitle.MaxLength));
        Description = new Description(Text.Truncate(signal.Description ?? string.Empty, Description.MaxLength));
        ServiceLabel = signal.ServiceLabel;
        Severity = Guard.Defined(signal.Severity, "severity");
    }

    public IncidentSource Source { get; }

    public AlertFingerprint Fingerprint { get; }

    public AlertStatus Status { get; }

    public string Name { get; }

    public IncidentTitle Title { get; }

    public Description Description { get; }

    public string? ServiceLabel { get; }

    public Severity Severity { get; }

    public bool IsFiring => Status == AlertStatus.Firing;

    public string TimelineMessage => $"Alert {Status.ToString().ToUpperInvariant()}: {Name}.";

    public static Alert From(AlertSignal signal) => new(signal);
}
