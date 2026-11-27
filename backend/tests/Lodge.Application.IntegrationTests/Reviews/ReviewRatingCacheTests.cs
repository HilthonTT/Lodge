using Lodge.Application.Apartments.GetById;
using Lodge.Application.Hosting.Bookings;
using Lodge.Application.Hosting.Bookings.CreateGuestReview;
using Lodge.Application.IntegrationTests.Abstractions;
using Lodge.Application.Reviews.Create;
using Lodge.Application.Reviews.Remove;
using Lodge.Application.Reviews.Update;
using Lodge.Contracts.Apartments;
using Lodge.Contracts.Reviews;
using Lodge.Domain.Apartments;
using Lodge.Domain.Bookings;
using Lodge.Domain.Core.Primitives;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lodge.Application.IntegrationTests.Reviews;

public class ReviewRatingCacheTests(IntegrationTestWebFactory factory) : BaseIntegrationTest(factory)
{
    private static readonly CategoryRatingsDto Categories = new()
    {
        Cleanliness = 1,
        Accuracy = 2,
        CheckIn = 3,
        Communication = 4,
        Location = 5,
        Value = 1,
    };

    [Fact]
    public async Task CachedApartment_Should_ReflectPublishedReviewChanges()
    {
        Guid userId = await CreateAndSignInUserAsync();
        Apartment apartment = await DbContext.Apartments.OrderBy(a => a.Name.Value).FirstAsync();
        Guid bookingId = await AddCompletedBookingAsync(apartment, userId);

        // Prime the cache.
        ApartmentResponse before = (await Sender.Send(new GetApartmentByIdQuery(apartment.Id))).Value;
        (await Sender.Send(new GetApartmentByIdQuery(apartment.Id))).Value.ReviewCount.Should().Be(before.ReviewCount);

        Result<Guid> created = await Sender.Send(
            new CreateReviewCommand(Guid.NewGuid(), userId, bookingId, 1, "Too noisy at night.", Categories));
        created.IsSuccess.Should().BeTrue();

        // Double-blind: the review is not published (nor counted) until the host has reviewed the guest.
        (await Sender.Send(new GetApartmentByIdQuery(apartment.Id))).Value.ReviewCount.Should().Be(before.ReviewCount);

        // Unpublished reviews can still be changed.
        (await Sender.Send(new UpdateReviewCommand(userId, created.Value, 2, "Noisy, but nice.", Categories))).IsSuccess
            .Should().BeTrue();

        Factory.CurrentUser.UserId = apartment.OwnerId;
        (await Sender.Send(new CreateGuestReviewCommand(bookingId, 5, "A lovely guest."))).IsSuccess.Should().BeTrue();
        Factory.CurrentUser.UserId = userId;

        ApartmentResponse afterPublish = (await Sender.Send(new GetApartmentByIdQuery(apartment.Id))).Value;
        afterPublish.ReviewCount.Should().Be(before.ReviewCount + 1);
        afterPublish.AverageRating.Should().NotBeNull();
        afterPublish.RatingBreakdown.Should().NotBeNull();
        if (before.AverageRating is not null)
        {
            afterPublish.AverageRating.Should().BeLessThan(before.AverageRating.Value);
        }

        // Published reviews are final.
        Result update = await Sender.Send(new UpdateReviewCommand(userId, created.Value, 5, "Actually lovely.", Categories));
        update.Error.Code.Should().Be("Review.AlreadyPublished");

        // ...and permanent: removing one would let the guest re-post after reading the host's review.
        Result remove = await Sender.Send(new RemoveReviewCommand(created.Value));
        remove.Error.Code.Should().Be("Review.AlreadyPublished");

        ApartmentResponse afterRemove = (await Sender.Send(new GetApartmentByIdQuery(apartment.Id))).Value;
        afterRemove.ReviewCount.Should().Be(afterPublish.ReviewCount);
        afterRemove.AverageRating.Should().Be(afterPublish.AverageRating);
    }

    private async Task<Guid> AddCompletedBookingAsync(Apartment apartment, Guid userId)
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        DateRange duration = DateRange.Create(today.AddDays(-3), today.AddDays(-1)).Value;
        Booking booking = Booking.Reserve(
            apartment,
            userId,
            duration,
            1,
            null,
            DateTime.UtcNow.AddDays(-10),
            Factory.Services.GetRequiredService<PricingService>().CalculatePrice(apartment, duration)).Value;

        if (booking.Status == BookingStatus.PendingApproval)
        {
            // A request to book: the host accepted it before the stay.
            booking.Accept(duration.Start.AddDays(-1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)).IsSuccess.Should().BeTrue();
        }

        booking.Confirm(DateTime.UtcNow);
        booking.Complete(DateTime.UtcNow).IsSuccess.Should().BeTrue();
        booking.ClearDomainEvents();

        DbContext.Bookings.Add(booking);
        await DbContext.SaveChangesAsync();

        return booking.Id;
    }
}
