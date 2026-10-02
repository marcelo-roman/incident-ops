using IncidentOps.Application.Common;
using IncidentOps.Application.Incidents;
using IncidentOps.Domain.Errors;
using Microsoft.AspNetCore.Mvc;

namespace IncidentOps.Api.Errors;

internal static class ExceptionProblemMapper
{
    public static ProblemDetails? Map(Exception exception) => exception switch
    {
        DomainValidationException validation => Validation(validation.Field, validation.Message),
        RequestValidationException validation => Validation(validation.Field, validation.Message),
        IncidentNotFoundException notFound => Problem(StatusCodes.Status404NotFound, "Incident not found", notFound.Message),
        InvalidStatusTransitionException transition => Problem(StatusCodes.Status409Conflict, "Invalid status transition", transition.Message),
        EscalationNotAllowedException escalation => Problem(StatusCodes.Status409Conflict, "Escalation not allowed", escalation.Message),
        ConcurrencyConflictException conflict => Problem(StatusCodes.Status409Conflict, "Concurrent update", conflict.Message),
        _ => null,
    };

    private static HttpValidationProblemDetails Validation(string field, string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] })
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation failed",
            Detail = message,
        };

    private static ProblemDetails Problem(int status, string title, string detail) =>
        new() { Status = status, Title = title, Detail = detail };
}
