using IncidentOps.Domain.Incidents;

namespace IncidentOps.Infrastructure.Seeding;

internal sealed record IncidentPlan(DateTimeOffset CreatedAt, IncidentTheme Theme, IncidentDraft Draft);
