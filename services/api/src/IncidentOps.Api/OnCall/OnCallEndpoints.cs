using IncidentOps.Application.OnCall;
using Microsoft.AspNetCore.Http.HttpResults;

namespace IncidentOps.Api.OnCall;

internal static class OnCallEndpoints
{
    public static RouteGroupBuilder MapOnCallEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/oncall/current", GetCurrent).WithName("GetCurrentOnCall").WithTags("On-call");
        return api;
    }

    private static async Task<Ok<OnCallView>> GetCurrent(
        GetCurrentOnCallHandler handler,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.HandleAsync(cancellationToken));
}
