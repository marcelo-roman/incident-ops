using IncidentOps.Escalation.Domain.Escalation;
using IncidentOps.Escalation.Domain.Watches;

namespace IncidentOps.Escalation.Domain.Paging;

public abstract record PagingDecision
{
    private PagingDecision(string description) => Description = description;

    public string Description { get; }

    public static PagingDecision Decide(AcknowledgementWindowOpened window, OnCallRotation rotation)
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(rotation);
        if (!window.Incident.Severity.PagesOnCall)
        {
            return new DoNotPage($"{window.Incident.Severity} does not page on call");
        }

        if (!window.IsAwaitingAcknowledgement)
        {
            return new DoNotPage("incident already acknowledged");
        }

        return new Page(new PageRequest(window.Incident, window.Level, rotation.TargetFor(window.Level)));
    }

    public sealed record Page : PagingDecision
    {
        internal Page(PageRequest request)
            : base($"page {request.Target} at level {request.Level}") => Request = request;

        public PageRequest Request { get; }
    }

    public sealed record DoNotPage : PagingDecision
    {
        internal DoNotPage(string reason)
            : base(reason)
        {
        }
    }
}
