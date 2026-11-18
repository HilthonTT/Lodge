using Lodge.Domain.Bookings;
using Lodge.Domain.Core.Primitives;

namespace Lodge.Domain.UnitTests.Bookings;

public class DateRangeTests
{
    [Fact]
    public void Create_Should_ReturnLengthInNights()
    {
        DateRange range = DateRange.Create(new DateOnly(2026, 1, 30), new DateOnly(2026, 2, 2)).Value;

        range.LengthInDays.Should().Be(3);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_Should_Fail_WhenEndIsNotAfterStart(int days)
    {
        var start = new DateOnly(2026, 1, 30);

        Result<DateRange> result = DateRange.Create(start, start.AddDays(days));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DateRangeErrors.StartDatePrecedesEndDate);
    }
}
