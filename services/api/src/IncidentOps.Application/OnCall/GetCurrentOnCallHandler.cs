using IncidentOps.Application.Common;
using IncidentOps.Domain.OnCall;

namespace IncidentOps.Application.OnCall;

public sealed class GetCurrentOnCallHandler(IOnCallRotationRepository rotations, IClock clock)
{
    public async Task<OnCallView> HandleAsync(CancellationToken cancellationToken)
    {
        var rotation = await rotations.GetAsync(cancellationToken);
        var shift = rotation.ShiftAt(clock.UtcNow);
        return new OnCallView(shift.WeekStart, shift.Primary.Value, shift.Secondary.Value, shift.Lead.Value);
    }
}
