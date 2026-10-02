using System.Text;
using IncidentOps.Escalation.Application.Messaging;

namespace IncidentOps.Functions.Tests.AntiCorruption;

internal static class CloudEvents
{
    public static string Incident(
        string type = "incident.triggered",
        string status = "Triggered",
        int level = 1,
        string severity = "Sev1") => $$"""
        {
          "specversion": "1.0",
          "id": "0b5e3a52-1d7b-4c43-9a55-6f1f0f7c2d10",
          "type": "{{type}}",
          "source": "incident-ops-api",
          "time": "2026-10-02T14:00:00Z",
          "subject": "{{Sample.IncidentGuid}}",
          "datacontenttype": "application/json",
          "data": {
            "id": "{{Sample.IncidentGuid}}",
            "number": 1042,
            "title": "Checkout returns 502",
            "description": "Upstream timeout",
            "serviceId": "checkout",
            "severity": "{{severity}}",
            "status": "{{status}}",
            "assignee": null,
            "escalationLevel": {{level}},
            "createdAt": "2026-10-02T13:45:00Z",
            "ackDueAt": "2026-10-02T14:15:00Z",
            "resolveDueAt": "2026-10-02T18:00:00Z",
            "slaState": "OnTrack"
          }
        }
        """;

    public static InboundMessage Message(string body, DateTimeOffset? scheduledFor = null) =>
        new("message-1", Encoding.UTF8.GetBytes(body), scheduledFor ?? Sample.Now);
}
