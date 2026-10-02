using IncidentOps.Api.Errors;
using IncidentOps.Api.Hosting;
using IncidentOps.Api.Security;
using IncidentOps.Application.Incidents;
using IncidentOps.Application.Incidents.Acknowledge;
using IncidentOps.Application.Incidents.Escalate;
using IncidentOps.Application.Incidents.Mitigate;
using IncidentOps.Application.Incidents.Notes;
using IncidentOps.Application.Incidents.Resolve;
using IncidentOps.Application.Incidents.Trigger;
using Microsoft.AspNetCore.Http.HttpResults;

namespace IncidentOps.Api.Incidents;

internal static class IncidentCommandEndpoints
{
    public static RouteGroupBuilder MapIncidentCommandEndpoints(this RouteGroupBuilder incidents)
    {
        var commands = incidents.MapGroup(string.Empty).RequireRateLimiting(RateLimitingRegistration.WritesPolicy);

        commands.MapPost("/", Trigger).WithName("TriggerIncident").WithCommandProblems();
        commands.MapPost("/{id:guid}/acknowledge", Acknowledge).WithName("AcknowledgeIncident").WithCommandProblems();
        commands.MapPost("/{id:guid}/escalate", Escalate).WithName("EscalateIncident").WithCommandProblems().RequireApiKey();
        commands.MapPost("/{id:guid}/mitigate", Mitigate).WithName("MitigateIncident").WithCommandProblems();
        commands.MapPost("/{id:guid}/resolve", Resolve).WithName("ResolveIncident").WithCommandProblems();
        commands.MapPost("/{id:guid}/notes", AddNote).WithName("AddIncidentNote").WithCommandProblems();
        return incidents;
    }

    private static async Task<Created<IncidentView>> Trigger(
        TriggerIncident command,
        TriggerIncidentHandler handler,
        CancellationToken cancellationToken)
    {
        var incident = await handler.HandleAsync(command, cancellationToken);
        return TypedResults.Created($"/api/incidents/{incident.Id}", incident);
    }

    private static async Task<Ok<IncidentView>> Acknowledge(
        Guid id,
        AcknowledgeIncident command,
        AcknowledgeIncidentHandler handler,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.HandleAsync(id, command, cancellationToken));

    private static async Task<Ok<IncidentView>> Escalate(
        Guid id,
        EscalateIncident command,
        EscalateIncidentHandler handler,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.HandleAsync(id, command, cancellationToken));

    private static async Task<Ok<IncidentView>> Mitigate(
        Guid id,
        MitigateIncident command,
        MitigateIncidentHandler handler,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.HandleAsync(id, command, cancellationToken));

    private static async Task<Ok<IncidentView>> Resolve(
        Guid id,
        ResolveIncident command,
        ResolveIncidentHandler handler,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.HandleAsync(id, command, cancellationToken));

    private static async Task<Ok<TimelineEntryView>> AddNote(
        Guid id,
        AddIncidentNote command,
        AddIncidentNoteHandler handler,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.HandleAsync(id, command, cancellationToken));
}
