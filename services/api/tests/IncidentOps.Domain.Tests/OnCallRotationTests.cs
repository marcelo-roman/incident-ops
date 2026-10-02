using IncidentOps.Domain.Errors;
using IncidentOps.Domain.Incidents;
using IncidentOps.Domain.OnCall;

namespace IncidentOps.Domain.Tests;

public class OnCallRotationTests
{
    private static readonly Engineer[] Roster =
    [
        .. Enumerable.Range(0, 6).Select(index => new Engineer($"e{index}", $"E{index}", EngineerRole.Rotation, index)),
        new Engineer("lead", "Lead", EngineerRole.Lead, 0),
    ];

    private static readonly OnCallRotation Rotation = new(Roster, TestData.NewYork);

    private static DateTimeOffset NewYorkTime(int year, int month, int day, int hour, int minute)
    {
        var local = new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Unspecified);
        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local, TestData.NewYork), TimeSpan.Zero);
    }

    [Fact]
    public void First_shift_starts_on_the_anchor_monday()
    {
        var shift = Rotation.ShiftAt(NewYorkTime(2024, 1, 1, 10, 30));

        shift.Primary.Should().Be(new Actor("E0"));
        shift.Secondary.Should().Be(new Actor("E1"));
        shift.Lead.Should().Be(new Actor("Lead"));
        shift.WeekStart.Should().Be(new DateTimeOffset(2024, 1, 1, 15, 30, 0, TimeSpan.Zero));
    }

    [Fact]
    public void Shift_changes_at_monday_ten_thirty_new_york_time()
    {
        Rotation.ShiftAt(NewYorkTime(2024, 1, 8, 10, 29)).Primary.Value.Should().Be("E0");
        Rotation.ShiftAt(NewYorkTime(2024, 1, 8, 10, 30)).Primary.Value.Should().Be("E1");
    }

    [Fact]
    public void Each_engineer_is_primary_once_every_six_weeks()
    {
        var primaries = Enumerable.Range(0, 12)
            .Select(week => Rotation.ShiftAt(NewYorkTime(2024, 1, 1, 12, 0).AddDays(7 * week)).Primary.Value)
            .ToList();

        primaries.Should().Equal("E0", "E1", "E2", "E3", "E4", "E5", "E0", "E1", "E2", "E3", "E4", "E5");
    }

    [Fact]
    public void Secondary_is_next_week_primary()
    {
        var shift = Rotation.ShiftAt(NewYorkTime(2024, 2, 5, 12, 0));

        shift.Primary.Value.Should().Be("E5");
        shift.Secondary.Value.Should().Be("E0");
    }

    [Fact]
    public void Week_start_follows_daylight_saving_time()
    {
        Rotation.ShiftAt(NewYorkTime(2026, 7, 15, 9, 0)).WeekStart
            .Should().Be(new DateTimeOffset(2026, 7, 13, 14, 30, 0, TimeSpan.Zero));
    }

    [Fact]
    public void Instants_before_the_anchor_wrap_backwards()
    {
        Rotation.ShiftAt(NewYorkTime(2023, 12, 30, 12, 0)).Primary.Value.Should().Be("E5");
    }

    [Fact]
    public void Rotation_needs_two_engineers_and_a_lead()
    {
        var withoutMembers = () => new OnCallRotation([new Engineer("solo", "Solo", EngineerRole.Rotation, 0), Roster[^1]], TestData.NewYork);
        var withoutLead = () => new OnCallRotation(Roster[..6], TestData.NewYork);

        withoutMembers.Should().Throw<DomainValidationException>();
        withoutLead.Should().Throw<DomainValidationException>().Which.Field.Should().Be("lead");
    }
}
