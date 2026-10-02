namespace IncidentOps.Application.OnCall;

public sealed record OnCallView(DateTimeOffset WeekStart, string Primary, string Secondary, string Lead);
