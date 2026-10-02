using IncidentOps.Application.Incidents;
using IncidentOps.Application.Incidents.Details;
using IncidentOps.Application.Incidents.Export;
using IncidentOps.Application.Incidents.Listing;
using Microsoft.AspNetCore.Http.HttpResults;

namespace IncidentOps.Api.Incidents;

internal static class IncidentQueryEndpoints
{
    public static RouteGroupBuilder MapIncidentQueryEndpoints(this RouteGroupBuilder incidents)
    {
        incidents.MapGet("/", ListIncidents).WithName("ListIncidents");
        incidents.MapGet("/export", ExportIncidents).WithName("ExportIncidents");
        incidents.MapGet("/{id:guid}", GetIncident).WithName("GetIncident").ProducesProblem(StatusCodes.Status404NotFound);
        return incidents;
    }

    private static async Task<Ok<IReadOnlyList<IncidentView>>> ListIncidents(
        [AsParameters] ListIncidents query,
        ListIncidentsHandler handler,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.HandleAsync(query, cancellationToken));

    private static async Task<Ok<IReadOnlyList<IncidentView>>> ExportIncidents(
        [AsParameters] ExportIncidents query,
        ExportIncidentsHandler handler,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.HandleAsync(query, cancellationToken));

    private static async Task<Ok<IncidentDetailsView>> GetIncident(
        Guid id,
        GetIncidentHandler handler,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.HandleAsync(id, cancellationToken));
}
