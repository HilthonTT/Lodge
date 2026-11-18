using Lodge.Application.Experiences;
using Lodge.Contracts.Experiences;
using Lodge.Domain.Experiences;

namespace Lodge.Application.UnitTests.Experiences;

public class SessionScheduleTests
{
    [Fact]
    public void Expand_Should_ConvertLocalStartTimes_AcrossDaylightSavingChanges()
    {
        // Amsterdam leaves summer time on 2026-10-25: 10:00 local is 08:00 UTC before and 09:00 UTC after.
        var recurring = new RecurringSessionsRequest
        {
            DaysOfWeek = ["Saturday", "sunday"],
            StartTime = "10:00",
            TimeZone = "Europe/Amsterdam",
            From = new DateOnly(2026, 10, 24),
            Until = new DateOnly(2026, 11, 1),
        };

        List<DateTime> starts = SessionSchedule.Expand(recurring).Value;

        starts.Should().Equal(
            new DateTime(2026, 10, 24, 8, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 10, 25, 9, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 10, 31, 9, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 11, 1, 9, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void Expand_Should_MoveStartTimesOutOfDaylightSavingGaps()
    {
        // 2026-03-29 02:30 does not exist in Amsterdam (clocks jump from 02:00 to 03:00).
        var recurring = new RecurringSessionsRequest
        {
            DaysOfWeek = ["Sunday"],
            StartTime = "02:30",
            TimeZone = "Europe/Amsterdam",
            From = new DateOnly(2026, 3, 29),
            Until = new DateOnly(2026, 3, 29),
        };

        SessionSchedule.Expand(recurring).Value.Should().Equal(new DateTime(2026, 3, 29, 1, 0, 0, DateTimeKind.Utc));
    }

    [Theory]
    [InlineData("Funday", "10:00", "Europe/Amsterdam")]
    [InlineData("Monday", "25:00", "Europe/Amsterdam")]
    [InlineData("Monday", "10:00", "")]
    public void Expand_Should_RejectInvalidSchedules(string day, string time, string timeZone)
    {
        var recurring = new RecurringSessionsRequest
        {
            DaysOfWeek = [day],
            StartTime = time,
            TimeZone = timeZone,
            From = new DateOnly(2026, 1, 1),
            Until = new DateOnly(2026, 2, 1),
        };

        SessionSchedule.Expand(recurring).IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Expand_Should_RejectReversedOrTooLongRanges()
    {
        var recurring = new RecurringSessionsRequest
        {
            DaysOfWeek = ["Monday"],
            StartTime = "10:00",
            TimeZone = "UTC",
            From = new DateOnly(2026, 2, 1),
            Until = new DateOnly(2026, 1, 1),
        };

        SessionSchedule.Expand(recurring).Error.Should().Be(ExperienceSessionErrors.InvalidRecurrence);
        SessionSchedule.Expand(recurring with { Until = new DateOnly(2027, 6, 1) }).Error.Should().Be(ExperienceSessionErrors.InvalidRecurrence);
    }
}
