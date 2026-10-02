using IncidentOps.Application.Common;
using IncidentOps.Application.Incidents;
using IncidentOps.Application.Incidents.Acknowledge;
using IncidentOps.Application.Incidents.Escalate;
using IncidentOps.Application.Incidents.Messaging;
using IncidentOps.Application.Incidents.Mitigate;
using IncidentOps.Application.Incidents.Notes;
using IncidentOps.Application.Incidents.Resolve;
using IncidentOps.Application.Incidents.Trigger;
using IncidentOps.Application.Tests.Fakes;
using IncidentOps.Domain.Errors;
using IncidentOps.Domain.Incidents;
using IncidentOps.Domain.Sla;

namespace IncidentOps.Application.Tests;

public sealed class IncidentUseCaseTests : IDisposable
{
    private readonly ApplicationHarness _harness = new();

    public void Dispose() => _harness.Dispose();

    [Fact]
    public async Task Trigger_adds_the_aggregate_commits_once_and_writes_one_outbox_entry()
    {
        var incident = await TriggerAsync(Severity.Sev1);

        incident.Number.Should().Be(2001);
        incident.Assignee.Should().BeOneOf(FakeOnCallRotationRepository.Engineers);
        incident.AckDueAt.Should().Be(_harness.Clock.UtcNow.AddMinutes(15));
        _harness.Incidents.All.Should().ContainSingle();
        _harness.UnitOfWork.Commits.Should().Be(1);
        _harness.UnitOfWork.Outbox.Should().ContainSingle().Which.Type.Should().Be(IncidentChangeMessage.MessageType);
        _harness.Incidents.All[0].DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task Nothing_is_published_until_the_outbox_is_dispatched()
    {
        await TriggerAsync(Severity.Sev1);

        _harness.Events.Events.Should().BeEmpty();
        await _harness.DispatchOutboxAsync();

        _harness.Events.Types.Should().Equal(IncidentEventTypes.Triggered);
        _harness.Notifier.Messages.Should().ContainSingle().Which.Entry.Kind.Should().Be(TimelineKind.Triggered);
    }

    [Fact]
    public async Task Trigger_rejects_unknown_services_without_committing()
    {
        var act = () => _harness.Get<TriggerIncidentHandler>().HandleAsync(
            new TriggerIncident("Title", "Description", "unknown", Severity.Sev2),
            CancellationToken.None);

        (await act.Should().ThrowAsync<RequestValidationException>()).Which.Field.Should().Be("serviceId");
        _harness.Incidents.All.Should().BeEmpty();
        _harness.UnitOfWork.Commits.Should().Be(0);
    }

    [Fact]
    public async Task Trigger_rejects_invalid_titles_through_the_value_object()
    {
        var act = () => _harness.Get<TriggerIncidentHandler>().HandleAsync(
            new TriggerIncident(" ", "Description", "checkout", Severity.Sev2),
            CancellationToken.None);

        (await act.Should().ThrowAsync<DomainValidationException>()).Which.Field.Should().Be("title");
    }

    [Fact]
    public async Task Time_scale_compresses_due_dates()
    {
        using var compressed = new ApplicationHarness(timeScale: 60);

        var incident = await compressed.Get<TriggerIncidentHandler>().HandleAsync(
            new TriggerIncident("Title", "Description", "checkout", Severity.Sev1),
            CancellationToken.None);

        incident.AckDueAt.Should().Be(compressed.Clock.UtcNow.AddSeconds(15));
        incident.ResolveDueAt.Should().Be(compressed.Clock.UtcNow.AddMinutes(4));
    }

    [Fact]
    public async Task Lifecycle_use_cases_publish_one_integration_event_per_transition()
    {
        var incident = await TriggerAsync(Severity.Sev2);

        await _harness.Get<AcknowledgeIncidentHandler>().HandleAsync(incident.Id, new AcknowledgeIncident("Ava"), CancellationToken.None);
        await _harness.Get<MitigateIncidentHandler>().HandleAsync(incident.Id, new MitigateIncident("Ava", "Rolled back"), CancellationToken.None);
        var resolved = await _harness.Get<ResolveIncidentHandler>().HandleAsync(incident.Id, new ResolveIncident("Ava", "Config"), CancellationToken.None);
        await _harness.DispatchOutboxAsync();

        resolved.Status.Should().Be(IncidentStatus.Resolved);
        resolved.SlaState.Should().Be(SlaState.Met);
        _harness.Events.Types.Should().Equal(
            IncidentEventTypes.Triggered,
            IncidentEventTypes.Acknowledged,
            IncidentEventTypes.Mitigated,
            IncidentEventTypes.Resolved);
        _harness.Events.Events.Select(integrationEvent => integrationEvent.Id).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task Integration_events_carry_the_snapshot_at_event_time()
    {
        var incident = await TriggerAsync(Severity.Sev2);
        await _harness.Get<AcknowledgeIncidentHandler>().HandleAsync(incident.Id, new AcknowledgeIncident("Ava"), CancellationToken.None);

        await _harness.DispatchOutboxAsync();

        _harness.Events.Events[0].Incident.Status.Should().Be(IncidentStatus.Triggered);
        _harness.Events.Events[1].Incident.Status.Should().Be(IncidentStatus.Acknowledged);
    }

    [Fact]
    public async Task Commands_on_unknown_incidents_fail_with_not_found()
    {
        var act = () => _harness.Get<AcknowledgeIncidentHandler>().HandleAsync(Guid.NewGuid(), new AcknowledgeIncident("Ava"), CancellationToken.None);

        await act.Should().ThrowAsync<IncidentNotFoundException>();
    }

    [Fact]
    public async Task Escalation_targets_the_secondary_and_records_the_acknowledgement_breach()
    {
        var incident = await TriggerAsync(Severity.Sev1);
        _harness.Clock.Advance(TimeSpan.FromMinutes(15));

        var escalated = await _harness.Get<EscalateIncidentHandler>().HandleAsync(incident.Id, new EscalateIncident("No ack"), CancellationToken.None);
        await _harness.DispatchOutboxAsync();

        escalated.EscalationLevel.Should().Be(2);
        escalated.Assignee.Should().NotBe(incident.Assignee).And.BeOneOf(FakeOnCallRotationRepository.Engineers);
        escalated.AckDueAt.Should().Be(_harness.Clock.UtcNow.AddMinutes(15));
        escalated.AcknowledgementBreached.Should().BeTrue();
        _harness.Events.Types.Should().EndWith(IncidentEventTypes.Escalated);
    }

    [Fact]
    public async Task Escalation_of_an_acknowledged_incident_is_rejected_by_the_aggregate()
    {
        var incident = await TriggerAsync(Severity.Sev1);
        await _harness.Get<AcknowledgeIncidentHandler>().HandleAsync(incident.Id, new AcknowledgeIncident("Ava"), CancellationToken.None);

        var act = () => _harness.Get<EscalateIncidentHandler>().HandleAsync(incident.Id, new EscalateIncident("No ack"), CancellationToken.None);

        await act.Should().ThrowAsync<EscalationNotAllowedException>();
        _harness.UnitOfWork.Commits.Should().Be(2);
    }

    [Fact]
    public async Task Notes_notify_without_integration_events()
    {
        var incident = await TriggerAsync(Severity.Sev3);

        var note = await _harness.Get<AddIncidentNoteHandler>().HandleAsync(incident.Id, new AddIncidentNote("Mei", "Investigating"), CancellationToken.None);
        await _harness.DispatchOutboxAsync();

        note.Kind.Should().Be(TimelineKind.Note);
        note.Message.Should().Be("Investigating");
        _harness.Events.Types.Should().Equal(IncidentEventTypes.Triggered);
        _harness.Notifier.Messages.Should().HaveCount(2);
    }

    [Fact]
    public async Task Failed_publication_can_be_retried_with_the_same_event_id()
    {
        await TriggerAsync(Severity.Sev2);
        var entry = _harness.UnitOfWork.Outbox.Single();
        var handler = _harness.Get<Common.Outbox.IOutboxMessageHandler>();
        _harness.Events.FailuresLeft = 1;

        var first = () => handler.HandleAsync(entry.Type, entry.Payload, CancellationToken.None);
        await first.Should().ThrowAsync<InvalidOperationException>();
        await handler.HandleAsync(entry.Type, entry.Payload, CancellationToken.None);

        _harness.Events.Events.Should().ContainSingle().Which.Id.Should().Be(entry.Id);
    }

    [Fact]
    public async Task Unknown_outbox_message_types_are_rejected()
    {
        var act = () => _harness.Get<Common.Outbox.IOutboxMessageHandler>().HandleAsync("other", "{}", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    private Task<IncidentView> TriggerAsync(Severity severity) =>
        _harness.Get<TriggerIncidentHandler>().HandleAsync(
            new TriggerIncident("Checkout latency", "p95 above objective", "checkout", severity),
            CancellationToken.None);
}
