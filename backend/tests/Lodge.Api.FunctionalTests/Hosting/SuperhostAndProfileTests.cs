using System.Net;
using System.Net.Http.Json;
using Lodge.Api.FunctionalTests.Abstractions;
using Lodge.Application.Hosting.Superhost;
using Lodge.Contracts.Apartments;
using Lodge.Contracts.Bookings;
using Lodge.Contracts.Common;
using Lodge.Contracts.Hosting;
using Lodge.Contracts.Users;
using Lodge.Domain.Bookings;
using Lodge.Domain.Reviews;
using Lodge.Domain.Users;
using Lodge.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lodge.Api.FunctionalTests.Hosting;

public class SuperhostAndProfileTests(FunctionalTestWebAppFactory factory) : HostingFunctionalTest(factory)
{
    [Fact]
    public async Task SeededHost_Should_BeASuperhost_WithAMatchingSummary()
    {
        (HttpClient sam, Guid samId) = await LoginAsHostAsync();

        HostingSummaryResponse? summary = await sam.GetFromJsonAsync<HostingSummaryResponse>("api/v1/hosting/summary");

        summary!.IsSuperhost.Should().BeTrue();
        summary.ListingCount.Should().Be(8);
        summary.PendingRequests.Should().BeGreaterThanOrEqualTo(2);
        summary.EarningsTotal.Should().BeGreaterThan(0);
        summary.Currency.Should().Be("USD");
        summary.ReviewCount.Should().Be(9);
        summary.AverageRating.Should().Be(4.89m);
        summary.Superhost.Should().BeEquivalentTo(new
        {
            CompletedStays = 10,
            AverageRating = 4.89m,
            MeetsRequirements = true,
            Requirements = new { MinCompletedStays = 5, MinAverageRating = 4.8m, MaxCancellationRate = 0.10m },
        });
        summary.Superhost.CancellationRate.Should().BeGreaterThan(0).And.BeLessThan(0.10m);

        UserProfileResponse? profile = await HttpClient.GetFromJsonAsync<UserProfileResponse>($"api/v1/users/{samId}/profile");
        profile!.IsSuperhost.Should().BeTrue();
        profile.HostingSince.Should().NotBeNull();
        profile.Listings.Should().HaveCount(8).And.OnlyContain(a => a.Host.Id == samId);
        profile.HostReviewCount.Should().Be(9);
        profile.HostAverageRating.Should().Be(4.9m);
    }

    [Fact]
    public async Task Profile_Should_ListOnlyListedApartments_AndBeEmptyForGuests()
    {
        (HttpClient host, Guid hostId, Guid listingId) = await CreateHostWithListingAsync();
        Guid unlistedId = await CreateListingAsync(host, listing => listing with { IsListed = false });

        UserProfileResponse? profile = await HttpClient.GetFromJsonAsync<UserProfileResponse>($"api/v1/users/{hostId}/profile");
        profile!.Listings.Select(a => a.Id).Should().Equal(listingId);
        profile.Listings.Should().NotContain(a => a.Id == unlistedId);
        profile.HostReviewCount.Should().Be(0);
        profile.HostAverageRating.Should().BeNull();
        profile.IsSuperhost.Should().BeFalse();

        (_, Guid guestId) = await CreateAuthenticatedUserAsync();
        UserProfileResponse? guest = await HttpClient.GetFromJsonAsync<UserProfileResponse>($"api/v1/users/{guestId}/profile");
        guest!.Listings.Should().BeEmpty();
        guest.HostingSince.Should().BeNull();
    }

    [Fact]
    public async Task SuperhostJob_Should_GrantAndRevokeTheStatus()
    {
        (HttpClient host, Guid hostId, Guid listingId) = await CreateHostWithListingAsync();
        (_, Guid priyaId) = await LoginAsync("priya.patel@lodge.dev", DemoPassword);
        (_, Guid samId) = await LoginAsHostAsync();
        (_, Guid guestId) = await CreateAuthenticatedUserAsync();

        await using (AsyncServiceScope scope = Factory.Services.CreateAsyncScope())
        {
            LodgeDbContext dbContext = scope.ServiceProvider.GetRequiredService<LodgeDbContext>();
            var pricing = new PricingService();
            var apartment = await dbContext.Apartments.SingleAsync(a => a.Id == listingId);
            DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);

            // Five completed five-star stays in the past.
            for (int stay = 0; stay < 5; stay++)
            {
                DateOnly start = today.AddDays(-100 + (stay * 10));
                DateRange duration = DateRange.Create(start, start.AddDays(2)).Value;
                Booking booking = Booking.Reserve(
                    apartment, guestId, duration, 1, null, DateTime.UtcNow, pricing.CalculatePrice(apartment, duration)).Value;
                booking.Confirm(DateTime.UtcNow);
                booking.Complete(DateTime.UtcNow).IsSuccess.Should().BeTrue();
                dbContext.Bookings.Add(booking);
                dbContext.Reviews.Add(Review.Create(booking, Rating.Create(5).Value, Comment.Create("Perfect!").Value).Value);
            }

            // Priya does not qualify (3 completed stays, average 3.7).
            (await dbContext.Users.SingleAsync(u => u.Id == priyaId)).SetSuperhost(true);

            foreach (var entity in dbContext.ChangeTracker.Entries<Domain.Core.Primitives.Entity>())
            {
                entity.Entity.ClearDomainEvents();
            }

            await dbContext.SaveChangesAsync();
        }

        await using (AsyncServiceScope scope = Factory.Services.CreateAsyncScope())
        {
            (await scope.ServiceProvider.GetRequiredService<IUpdateSuperhostStatusJob>().UpdateAsync())
                .Should().BeGreaterThanOrEqualTo(2);
        }

        await using (AsyncServiceScope scope = Factory.Services.CreateAsyncScope())
        {
            Dictionary<Guid, bool> superhosts = await scope.ServiceProvider.GetRequiredService<LodgeDbContext>().Users
                .Where(u => u.Id == hostId || u.Id == priyaId || u.Id == samId)
                .ToDictionaryAsync(u => u.Id, u => u.IsSuperhost);

            superhosts[hostId].Should().BeTrue();
            superhosts[priyaId].Should().BeFalse();
            superhosts[samId].Should().BeTrue();
        }

        // The badge shows on the listing (the cache was evicted) and in the summary.
        (await HttpClient.GetFromJsonAsync<ApartmentResponse>($"api/v1/apartments/{listingId}"))!.Host.IsSuperhost.Should().BeTrue();
        HostingSummaryResponse? summary = await host.GetFromJsonAsync<HostingSummaryResponse>("api/v1/hosting/summary");
        summary!.Should().BeEquivalentTo(new { IsSuperhost = true, ReviewCount = 5, AverageRating = 5m });
        summary.Superhost.Should().BeEquivalentTo(new { CompletedStays = 5, CancellationRate = 0m, MeetsRequirements = true });
        summary.EarningsTotal.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task HostBookings_Should_OnlyListBookingsOfTheCallersListings()
    {
        (HttpClient host, _, Guid listingId) = await CreateHostWithListingAsync();
        (_, _, Guid bookingId) = await BookAsGuestAsync(listingId, NextFreeDate(), 2);

        var bookings = await host.GetFromJsonAsync<PagedList<BookingResponse>>("api/v1/hosting/bookings");
        bookings!.Items.Select(b => b.Id).Should().Equal(bookingId);
        bookings.Items[0].GuestCount.Should().Be(1);

        (HttpClient other, _) = await CreateAuthenticatedUserAsync();
        (await other.GetFromJsonAsync<PagedList<BookingResponse>>("api/v1/hosting/bookings"))!
            .Items.Should().BeEmpty();

        await ShouldFailWithAsync(await host.GetAsync("api/v1/hosting/bookings?status=9"), HttpStatusCode.BadRequest, "GetBookings.InvalidStatus");
    }
}
