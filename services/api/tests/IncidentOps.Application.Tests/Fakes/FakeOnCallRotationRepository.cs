using IncidentOps.Domain.OnCall;

namespace IncidentOps.Application.Tests.Fakes;

public sealed class FakeOnCallRotationRepository : IOnCallRotationRepository
{
    public static readonly string[] Engineers = ["E0", "E1", "E2", "E3", "E4", "E5"];

    private static readonly OnCallRotation Rotation = new(
        [
            .. Engineers.Select((name, index) => new Engineer(name.ToUpperInvariant(), name, EngineerRole.Rotation, index)),
            new Engineer("lead", "Lead", EngineerRole.Lead, 0),
        ],
        TimeZoneInfo.FindSystemTimeZoneById(OnCallRotation.TimeZoneId));

    public Task<OnCallRotation> GetAsync(CancellationToken cancellationToken) => Task.FromResult(Rotation);
}
