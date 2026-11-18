using Lodge.Application.Bookings.Get;

namespace Lodge.Application.UnitTests.Bookings;

public class GetBookingsQueryValidatorTests
{
    [Theory]
    [InlineData(null, 1, 10, true)]
    [InlineData(1, 1, 10, true)]
    [InlineData(5, 2, 100, true)]
    [InlineData(0, 1, 10, false)]
    [InlineData(7, 1, 10, true)]
    [InlineData(8, 1, 10, false)]
    [InlineData(null, 0, 10, false)]
    [InlineData(null, 1, 101, false)]
    public void Validate_Should_CheckStatusAndPaging(int? status, int page, int pageSize, bool valid)
    {
        bool result = new GetBookingsQueryValidator().Validate(new GetBookingsQuery(status, page, pageSize)).IsValid;

        result.Should().Be(valid);
    }
}
