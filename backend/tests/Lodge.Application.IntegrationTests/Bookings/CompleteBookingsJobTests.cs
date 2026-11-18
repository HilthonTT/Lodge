using Lodge.Application.Bookings.Complete;
using Lodge.Application.IntegrationTests.Abstractions;
using Lodge.Domain.Apartments;
using Lodge.Domain.Bookings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lodge.Application.IntegrationTests.Bookings;

public class CompleteBookingsJobTests(IntegrationTestWebFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Job_Should_CompleteOnlyConfirmedBookings_ThatEndedBeforeToday()
    {
        Guid userId = await CreateAndSignInUserAsync();
        Apartment apartment = await DbContext.Apartments
            .Where(a => a.InstantBook && a.MinNights <= 2)
            .OrderByDescending(a => a.Id)
            .FirstAsync();
        PricingService pricingService = Factory.Services.GetRequiredService<PricingService>();
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);

        Booking AddBooking(DateOnly start, DateOnly end, bool confirm)
        {
            DateRange duration = DateRange.Create(start, end).Value;
            Booking booking = Booking.Reserve(
                apartment,
                userId,
                duration,
                1,
                null,
                DateTime.UtcNow,
                pricingService.CalculatePrice(apartment, duration)).Value;

            if (confirm)
            {
                booking.Confirm(DateTime.UtcNow).IsSuccess.Should().BeTrue();
            }

            DbContext.Bookings.Add(booking);

            return booking;
        }

        // Dates far from "today" (except where the boundary matters) so other tests' bookings never overlap.
        Booking endedYesterday = AddBooking(today.AddDays(-4), today.AddDays(-1), confirm: true);
        Booking endsToday = AddBooking(today.AddDays(-2), today, confirm: true);
        Booking reservedAndEnded = AddBooking(today.AddDays(-1000), today.AddDays(-997), confirm: false);
        Booking confirmedInFuture = AddBooking(today.AddDays(5000), today.AddDays(5003), confirm: true);
        await DbContext.SaveChangesAsync();

        await using (AsyncServiceScope scope = Factory.Services.CreateAsyncScope())
        {
            ICompleteBookingsJob job = scope.ServiceProvider.GetRequiredService<ICompleteBookingsJob>();

            (await job.CompleteFinishedBookingsAsync()).Should().BeGreaterThanOrEqualTo(1);
        }

        Dictionary<Guid, BookingStatus> statuses = await DbContext.Bookings
            .AsNoTracking()
            .Where(b => b.UserId == userId)
            .ToDictionaryAsync(b => b.Id, b => b.Status);

        statuses[endedYesterday.Id].Should().Be(BookingStatus.Completed);
        statuses[endsToday.Id].Should().Be(BookingStatus.Confirmed);
        statuses[reservedAndEnded.Id].Should().Be(BookingStatus.Reserved);
        statuses[confirmedInFuture.Id].Should().Be(BookingStatus.Confirmed);

        // Completion goes through the domain method, so the domain event lands in the outbox.
        List<string> outboxContents = await DbContext.Database
            .SqlQuery<string>($"SELECT content AS \"Value\" FROM outbox_messages")
            .ToListAsync();

        outboxContents.Should().Contain(content =>
            content.Contains(nameof(Domain.Bookings.Events.BookingCompletedDomainEvent), StringComparison.Ordinal) &&
            content.Contains(endedYesterday.Id.ToString(), StringComparison.OrdinalIgnoreCase));
    }
}
