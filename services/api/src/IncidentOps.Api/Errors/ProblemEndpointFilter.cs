namespace IncidentOps.Api.Errors;

internal sealed class ProblemEndpointFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try
        {
            return await next(context);
        }
        catch (Exception exception) when (ExceptionProblemMapper.Map(exception) is not null)
        {
            return TypedResults.Problem(ExceptionProblemMapper.Map(exception)!);
        }
    }
}
