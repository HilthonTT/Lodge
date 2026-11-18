using Lodge.Domain.Apartments;

namespace Lodge.Domain.UnitTests.Apartments;

public class FlexibleStayTests
{
    private static readonly YearMonth December2026 = new(2026, 12);
    private static readonly YearMonth January2027 = new(2027, 1);

    [Fact]
    public void CandidateCheckIns_Should_BeTheFridays_ForWeekends()
    {
        IReadOnlyList<DateOnly> checkIns = FlexibleStay.CandidateCheckIns(
            FlexibleStayLength.Weekend,
            [December2026],
            new DateOnly(2026, 11, 18));

        checkIns.Should().Equal(
            new DateOnly(2026, 12, 4),
            new DateOnly(2026, 12, 11),
            new DateOnly(2026, 12, 18),
            new DateOnly(2026, 12, 25));
    }

    [Fact]
    public void CandidateCheckIns_Should_SkipPastDays_AndCoverEveryMonthOnce()
    {
        IReadOnlyList<DateOnly> checkIns = FlexibleStay.CandidateCheckIns(
            FlexibleStayLength.Week,
            [January2027, December2026, December2026],
            new DateOnly(2026, 12, 20));

        checkIns.Should().HaveCount(12 + 31);
        checkIns[0].Should().Be(new DateOnly(2026, 12, 20));
        checkIns[^1].Should().Be(new DateOnly(2027, 1, 31));
    }

    [Fact]
    public void FindFirstWindow_Should_FindAFreeWeekend()
    {
        // The first two weekends of December are booked.
        HashSet<DateOnly> unavailable = [new(2026, 12, 5), new(2026, 12, 11)];

        var window = FlexibleStay.FindFirstWindow(
            FlexibleStayLength.Weekend,
            [December2026],
            new DateOnly(2026, 11, 18),
            night => !unavailable.Contains(night));

        window!.Start.Should().Be(new DateOnly(2026, 12, 18));
        window.End.Should().Be(new DateOnly(2026, 12, 20));
    }

    [Fact]
    public void FindFirstWindow_Should_AllowAMonthStartingAtTheEndOfAMonth()
    {
        // Everything is booked until December 30th; a 28-night stay starting on the 31st is still "in December".
        var window = FlexibleStay.FindFirstWindow(
            FlexibleStayLength.Month,
            [December2026],
            new DateOnly(2026, 11, 18),
            night => night >= new DateOnly(2026, 12, 31));

        window!.Start.Should().Be(new DateOnly(2026, 12, 31));
        window.LengthInDays.Should().Be(28);
    }

    [Fact]
    public void FindFirstWindow_Should_ReturnNull_WhenNoWindowFits()
    {
        // One blocked night every 7 days: no 7-night window is free.
        var window = FlexibleStay.FindFirstWindow(
            FlexibleStayLength.Week,
            [December2026],
            new DateOnly(2026, 11, 18),
            night => night.DayNumber % 7 != 0);

        window.Should().BeNull();
    }

    [Theory]
    [InlineData("2026-12", true)]
    [InlineData(" 2027-01 ", true)]
    [InlineData("2026-13", false)]
    [InlineData("12-2026", false)]
    [InlineData("", false)]
    public void YearMonth_Should_ParseYearAndMonth(string value, bool valid)
    {
        YearMonth.TryParse(value, out YearMonth month).Should().Be(valid);

        if (valid)
        {
            month.ToString().Should().Be(value.Trim());
        }
    }

    [Theory]
    [InlineData("weekend", FlexibleStayLength.Weekend, 2)]
    [InlineData("Week", FlexibleStayLength.Week, 7)]
    [InlineData("MONTH", FlexibleStayLength.Month, 28)]
    public void TryParseLength_Should_AcceptTheNames(string value, FlexibleStayLength expected, int nights)
    {
        FlexibleStay.TryParseLength(value, out FlexibleStayLength length).Should().BeTrue();

        length.Should().Be(expected);
        FlexibleStay.Nights(length).Should().Be(nights);
    }
}
