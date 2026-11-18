using Lodge.Application.Bookings.Complete;
using Lodge.Domain.Apartments;
using Lodge.Domain.Bookings;

namespace Lodge.Application.UnitTests.Bookings;

public class CompleteBookingsJobTests
{
    private static readonly DateOnly Today = new(2030, 6, 15);

    private static readonly Func<Booking, bool> IsDue = CompleteBookingsJob.IsDueForCompletion(Today).Compile();

    [Theory]
    [InlineData(-1, BookingStatus.Confirmed, true)]
    [InlineData(-30, BookingStatus.Confirmed, true)]
    [InlineData(0, BookingStatus.Confirmed, false)]
    [InlineData(1, BookingStatus.Confirmed, false)]
    [InlineData(-1, BookingStatus.Reserved, false)]
    [InlineData(-1, BookingStatus.Rejected, false)]
    [InlineData(-1, BookingStatus.Cancelled, false)]
    [InlineData(-1, BookingStatus.Completed, false)]
    public void IsDueForCompletion_Should_SelectConfirmedBookings_EndingBeforeToday(
        int endOffsetDays,
        BookingStatus status,
        bool expected)
    {
        Booking booking = CreateBooking(Today.AddDays(endOffsetDays), status);

        booking.Status.Should().Be(status);
        IsDue(booking).Should().Be(expected);
    }

    private static Booking CreateBooking(DateOnly end, BookingStatus status)
    {
        DateOnly start = end.AddDays(-3);
        Booking booking = TestApartments.Reserve(TestApartments.Create(), start, end, DateTime.UtcNow);

        DateTime beforeStart = start.AddDays(-1).ToDateTime(TimeOnly.MinValue);

        switch (status)
        {
            case BookingStatus.Confirmed:
                booking.Confirm(beforeStart);
                break;
            case BookingStatus.Completed:
                booking.Confirm(beforeStart);
                booking.Complete(end.AddDays(1).ToDateTime(TimeOnly.MinValue));
                break;
            case BookingStatus.Rejected:
                booking.Reject(beforeStart);
                break;
            case BookingStatus.Cancelled:
                booking.Cancel(beforeStart);
                break;
        }

        return booking;
    }
}
