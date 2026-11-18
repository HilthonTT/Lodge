using Lodge.Application.Apartments.GetById;
using Lodge.Application.IntegrationTests.Abstractions;
using Lodge.Application.Reviews.Create;
using Lodge.Application.Reviews.Remove;
using Lodge.Application.Reviews.Update;
using Lodge.Contracts.Apartments;
using Lodge.Domain.Apartments;
using Lodge.Domain.Bookings;
using Lodge.Domain.Core.Primitives;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lodge.Application.IntegrationTests.Reviews;

public class ReviewRatingCacheTests(IntegrationTestWebFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task CachedApartment_Should_ReflectReviewChanges()
    {
        Guid userId = await CreateAndSignInUserAsync();
        Apartment apartment = await DbContext.Apartments.OrderBy(a => a.Name.Value).FirstAsync();
        Guid bookingId = await AddCompletedBookingAsync(apartment, userId);

        // Prime the cache.
        ApartmentResponse before = (await Sender.Send(new GetApartmentByIdQuery(apartment.Id))).Value;
        (await Sender.Send(new GetApartmentByIdQuery(apartment.Id))).Value.ReviewCount.Should().Be(before.ReviewCount);

        Result<Guid> created = await Sender.Send(
            new CreateReviewCommand(Guid.NewGuid(), userId, bookingId, 1, "Too noisy at night."));
        created.IsSuccess.Should().BeTrue();

        ApartmentResponse afterCreate = (await Sender.Send(new GetApartmentByIdQuery(apartment.Id))).Value;
        afterCreate.ReviewCount.Should().Be(before.ReviewCount + 1);
        afterCreate.AverageRating.Should().NotBeNull();
        if (before.AverageRating is not null)
        {
            afterCreate.AverageRating.Should().BeLessThan(before.AverageRating.Value);
        }

        (await Sender.Send(new UpdateReviewCommand(userId, created.Value, 5, "Actually lovely."))).IsSuccess
            .Should().BeTrue();

        ApartmentResponse afterUpdate = (await Sender.Send(new GetApartmentByIdQuery(apartment.Id))).Value;
        afterUpdate.ReviewCount.Should().Be(afterCreate.ReviewCount);
        afterUpdate.AverageRating.Should().BeGreaterThan(afterCreate.AverageRating!.Value);

        (await Sender.Send(new RemoveReviewCommand(created.Value))).IsSuccess.Should().BeTrue();

        ApartmentResponse afterRemove = (await Sender.Send(new GetApartmentByIdQuery(apartment.Id))).Value;
        afterRemove.ReviewCount.Should().Be(before.ReviewCount);
        afterRemove.AverageRating.Should().Be(before.AverageRating);
    }

    private async Task<Guid> AddCompletedBookingAsync(Apartment apartment, Guid userId)
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        var booking = Booking.Reserve(
            apartment,
            userId,
            DateRange.Create(today.AddDays(-2000), today.AddDays(-1998)).Value,
            DateTime.UtcNow,
            Factory.Services.GetRequiredService<PricingService>());
        booking.Confirm(DateTime.UtcNow);
        booking.Complete(DateTime.UtcNow).IsSuccess.Should().BeTrue();

        DbContext.Bookings.Add(booking);
        await DbContext.SaveChangesAsync();

        return booking.Id;
    }
}
