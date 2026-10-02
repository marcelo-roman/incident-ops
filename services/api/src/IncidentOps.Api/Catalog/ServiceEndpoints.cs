using IncidentOps.Application.Catalog;
using Microsoft.AspNetCore.Http.HttpResults;

namespace IncidentOps.Api.Catalog;

internal static class ServiceEndpoints
{
    public static RouteGroupBuilder MapServiceEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/services", ListServices).WithName("ListServices").WithTags("Services");
        return api;
    }

    private static async Task<Ok<IReadOnlyList<ServiceView>>> ListServices(
        ListServicesHandler handler,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.HandleAsync(cancellationToken));
}
