using IncidentOps.Api.Security;
using IncidentOps.Application.Common;
using Microsoft.AspNetCore.Http.HttpResults;

namespace IncidentOps.Api.Chaos;

internal static class ChaosEndpoints
{
    public static RouteGroupBuilder MapChaosEndpoints(this RouteGroupBuilder api)
    {
        var chaos = api.MapGroup("/chaos").WithTags("Chaos").RequireApiKey();
        chaos.MapPost("/faults", Inject).WithName("InjectFault").ProducesValidationProblem();
        chaos.MapDelete("/faults", Clear).WithName("ClearFault");
        return api;
    }

    private static Ok<ActiveFault> Inject(FaultInjectionRequest request, FaultSwitch faults, IClock clock)
    {
        var fault = request.Activate(clock.UtcNow);
        faults.Activate(fault);
        return TypedResults.Ok(fault);
    }

    private static NoContent Clear(FaultSwitch faults)
    {
        faults.Clear();
        return TypedResults.NoContent();
    }
}
