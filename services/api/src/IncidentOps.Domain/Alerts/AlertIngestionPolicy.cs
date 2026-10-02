using IncidentOps.Domain.Incidents;

namespace IncidentOps.Domain.Alerts;

public static class AlertIngestionPolicy
{
    public static AlertAction Decide(Alert alert, Incident? openIncident)
    {
        if (openIncident is not null)
        {
            return AlertAction.AppendToIncident;
        }

        if (alert.IsFiring)
        {
            return AlertAction.OpenIncident;
        }

        return AlertAction.Ignore;
    }

    public static bool ShouldMitigate(AlertStatus status, IncidentStatus incidentStatus) =>
        status == AlertStatus.Resolved && IncidentStateMachine.CanTransition(incidentStatus, IncidentStatus.Mitigated);
}
