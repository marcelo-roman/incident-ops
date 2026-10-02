using IncidentOps.Domain.Errors;
using IncidentOps.Domain.Incidents;
using IncidentOps.Domain.Incidents.Events;

namespace IncidentOps.Domain.Tests;

public class IncidentAggregateTests
{
    [Fact]
    public void Trigger_pages_the_primary_starts_the_clock_and_raises_triggered()
    {
        var incident = TestData.Incident(Severity.Sev1);

        incident.Status.Should().Be(IncidentStatus.Triggered);
        incident.Number.Should().Be(new IncidentNumber(1001));
        incident.Assignee.Should().Be(new Actor("Primary"));
        incident.EscalationLevel.Should().Be(EscalationLevel.First);
        incident.Source.Should().Be(IncidentSource.Manual);
        incident.AlertFingerprint.Should().BeNull();
        incident.CreatedAt.Should().Be(TestData.Now);
        incident.Sla.AckDueAt.Should().Be(TestData.Now.AddMinutes(15));
        incident.Sla.ResolveDueAt.Should().Be(TestData.Now.AddHours(4));
        incident.Timeline.Should().ContainSingle().Which.Kind.Should().Be(TimelineKind.Triggered);
        incident.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<IncidentTriggered>()
            .Which.TimelineEntryId.Should().Be(incident.Timeline[0].Id);
    }

    [Fact]
    public void Each_behavior_raises_one_event_bound_to_its_timeline_entry()
    {
        var incident = TestData.Incident();
        incident.ClearDomainEvents();

        incident.Acknowledge(TestData.Ava, TestData.Now.AddMinutes(5));
        incident.AddNote(TestData.Ava, TestData.Note(), TestData.Now.AddMinutes(10));
        incident.Mitigate(TestData.Ava, TestData.Note("Rolled back"), TestData.Now.AddMinutes(30));
        incident.Resolve(TestData.Ava, TestData.RootCause(), TestData.Now.AddHours(1));

        incident.DomainEvents.Select(domainEvent => domainEvent.GetType()).Should().Equal(
            typeof(IncidentAcknowledged), typeof(IncidentNoteAdded), typeof(IncidentMitigated), typeof(IncidentResolved));
        incident.DomainEvents.Cast<IncidentDomainEvent>().Select(domainEvent => domainEvent.TimelineEntryId)
            .Should().Equal(incident.Timeline.Skip(1).Select(entry => entry.Id));
    }

    [Fact]
    public void Full_lifecycle_records_each_step()
    {
        var incident = TestData.Incident();

        incident.Acknowledge(TestData.Ava, TestData.Now.AddMinutes(5));
        incident.Mitigate(TestData.Ava, TestData.Note("Rolled back"), TestData.Now.AddMinutes(30));
        incident.Resolve(TestData.Ava, TestData.RootCause(), TestData.Now.AddHours(1));

        incident.Status.Should().Be(IncidentStatus.Resolved);
        incident.Assignee.Should().Be(TestData.Ava);
        incident.AcknowledgedAt.Should().Be(TestData.Now.AddMinutes(5));
        incident.MitigatedAt.Should().Be(TestData.Now.AddMinutes(30));
        incident.ResolvedAt.Should().Be(TestData.Now.AddHours(1));
        incident.RootCause.Should().Be(TestData.RootCause());
        incident.IsOpen.Should().BeFalse();
        incident.Timeline.Select(entry => entry.Sequence).Should().Equal(1, 2, 3, 4);
    }

    [Fact]
    public void Triggered_incident_can_be_mitigated_or_resolved_without_acknowledgement()
    {
        var mitigated = TestData.Incident();
        var resolved = TestData.Incident();

        mitigated.Mitigate(TestData.Ava, TestData.Note(), TestData.Now.AddMinutes(3));
        resolved.Resolve(TestData.Ava, TestData.RootCause("False positive"), TestData.Now.AddMinutes(3));

        mitigated.Status.Should().Be(IncidentStatus.Mitigated);
        resolved.Status.Should().Be(IncidentStatus.Resolved);
        resolved.AcknowledgedAt.Should().BeNull();
    }

    [Fact]
    public void Invalid_transition_is_rejected_without_raising_events()
    {
        var incident = TestData.Incident();
        incident.Resolve(TestData.Ava, TestData.RootCause(), TestData.Now);
        incident.ClearDomainEvents();

        var act = () => incident.Acknowledge(TestData.Ava, TestData.Now);

        act.Should().Throw<InvalidStatusTransitionException>();
        incident.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Unknown_severity_is_rejected()
    {
        var act = () => Incident.Trigger(TestData.Draft((Severity)42), TestData.Opening());

        act.Should().Throw<DomainValidationException>().Which.Field.Should().Be("severity");
    }

    [Fact]
    public void Notes_do_not_change_status()
    {
        var incident = TestData.Incident();

        incident.AddNote(TestData.Ava, TestData.Note("Looking at dashboards"), TestData.Now.AddMinutes(1));

        incident.Timeline[^1].Kind.Should().Be(TimelineKind.Note);
        incident.Timeline[^1].IncidentId.Should().Be(incident.Id);
        incident.Status.Should().Be(IncidentStatus.Triggered);
    }
}
