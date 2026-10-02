namespace IncidentOps.Application.Common;

public sealed class ConcurrencyConflictException(Exception innerException)
    : Exception("The incident was changed by another request. Reload it and retry.", innerException);
