using IncidentOps.Domain.Common;

namespace IncidentOps.Domain.OnCall;

public sealed class OnCallRotation : AggregateRoot<string>
{
    public const string DefaultId = "primary";
    public const string TimeZoneId = "America/New_York";
    public const int DaysPerShift = 7;
    public const int MinimumMembers = 2;

    public static readonly DateTime FirstShiftStartsAt = new(2024, 1, 1, 10, 30, 0, DateTimeKind.Unspecified);

    private readonly IReadOnlyList<Engineer> _members;
    private readonly Engineer _lead;
    private readonly TimeZoneInfo _timeZone;

    public OnCallRotation(IEnumerable<Engineer> engineers, TimeZoneInfo timeZone)
        : base(DefaultId)
    {
        var roster = engineers.ToList();
        _members = roster
            .Where(engineer => engineer.Role == EngineerRole.Rotation)
            .OrderBy(engineer => engineer.RotationOrder)
            .ToList();
        Guard.Against(_members.Count < MinimumMembers, "engineers", $"A rotation needs at least {MinimumMembers} engineers.");
        var lead = roster.FirstOrDefault(engineer => engineer.Role == EngineerRole.Lead);
        Guard.Against(lead is null, "lead", "A rotation needs an engineering lead.");
        _lead = lead!;
        _timeZone = timeZone;
    }

    public OnCallShift ShiftAt(DateTimeOffset instant)
    {
        var week = WeekIndexAt(instant);
        var startsAtLocal = FirstShiftStartsAt.AddDays(week * DaysPerShift);
        var startsAtUtc = TimeZoneInfo.ConvertTimeToUtc(startsAtLocal, _timeZone);
        return new OnCallShift(new DateTimeOffset(startsAtUtc), MemberAt(week).AsActor(), MemberAt(week + 1).AsActor(), _lead.AsActor());
    }

    private int WeekIndexAt(DateTimeOffset instant)
    {
        var local = TimeZoneInfo.ConvertTime(instant, _timeZone).DateTime;
        return (int)Math.Floor((local - FirstShiftStartsAt).TotalDays / DaysPerShift);
    }

    private Engineer MemberAt(int week)
    {
        var count = _members.Count;
        return _members[((week % count) + count) % count];
    }
}
