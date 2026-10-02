using IncidentOps.Domain.OnCall;
using Microsoft.EntityFrameworkCore;

namespace IncidentOps.Infrastructure.Persistence.Repositories;

internal sealed class OnCallRotationRepository(IncidentOpsDbContext db) : IOnCallRotationRepository
{
    private static readonly TimeZoneInfo RotationTimeZone = TimeZoneInfo.FindSystemTimeZoneById(OnCallRotation.TimeZoneId);

    public async Task<OnCallRotation> GetAsync(CancellationToken cancellationToken)
    {
        var engineers = await db.Engineers.AsNoTracking().ToListAsync(cancellationToken);
        return new OnCallRotation(engineers, RotationTimeZone);
    }
}
