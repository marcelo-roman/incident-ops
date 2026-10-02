namespace IncidentOps.Api.Errors;

internal static class ProblemResponses
{
    public static RouteHandlerBuilder WithCommandProblems(this RouteHandlerBuilder builder) =>
        builder
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

    public static RouteHandlerBuilder WithAlertProblems(this RouteHandlerBuilder builder) =>
        builder
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);
}
