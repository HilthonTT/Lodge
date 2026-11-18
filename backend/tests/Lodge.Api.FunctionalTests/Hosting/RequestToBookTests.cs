using System.Net;
using System.Net.Http.Json;
using Lodge.Api.FunctionalTests.Abstractions;
using Lodge.Application.Bookings.Expire;
using Lodge.Contracts.Bookings;
using Lodge.Contracts.Common;
using Lodge.Contracts.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Lodge.Api.FunctionalTests.Hosting;

public class RequestToBookTests(FunctionalTestWebAppFactory factory) : HostingFunctionalTest(factory)
{
    private const int Reserved = 1;
    private const int Confirmed = 2;
    private const int Rejected = 3;
    private const int Cancelled = 4;
    private const int PendingApproval = 6;
    private const int Expired = 7;

    [Fact]
    public async Task Booking_Should_RequireAGuestCountWithinTheListingsMaximum()
    {
        Guid apartmentId = await GetInstantBookApartmentIdAsync();
        (HttpClient guest, Guid guestId) = await CreateAuthenticatedUserAsync();
        DateOnly start = NextFreeDate();

        using var withoutGuests = new HttpRequestMessage(HttpMethod.Post, "api/v1/bookings")
        {
            Content = JsonContent.Create(new
            {
                apartmentId,
                userId = guestId,
                startDate = start,
                endDate = start.AddDays(2),
            }),
        };
        withoutGuests.Headers.Add("X-Idempotency-Key", Guid.NewGuid().ToString());
        await ShouldFailWithAsync(await guest.SendAsync(withoutGuests), HttpStatusCode.BadRequest, "ReserveBooking.GuestCountIsRequired");

        // The Canal Houseboat sleeps 2.
        await ShouldFailWithAsync(
            await PostBookingAsync(guest, guestId, apartmentId, start, 2, guestCount: 3),
            HttpStatusCode.BadRequest,
            "Booking.InvalidGuestCount");

        await ShouldFailWithAsync(
            await PostBookingAsync(guest, guestId, apartmentId, start, 2, messageToHost: new string('x', 1001)),
            HttpStatusCode.BadRequest,
            "Booking.MessageTooLong");

        HttpResponseMessage ok = await PostBookingAsync(guest, guestId, apartmentId, start, 2, guestCount: 2, messageToHost: "Hi!");
        ok.StatusCode.Should().Be(HttpStatusCode.OK);

        BookingResponse booking = await GetBookingAsync(guest, await ok.Content.ReadFromJsonAsync<Guid>());
        booking.Should().BeEquivalentTo(new { Status = Reserved, GuestCount = 2, MessageToHost = "Hi!", CheckInInstructions = (string?)null });
    }

    [Fact]
    public async Task Host_Should_AcceptAndDeclineBookingRequests()
    {
        (HttpClient host, Guid hostId, Guid listingId) = await CreateHostWithListingAsync(listing => listing with { InstantBook = false });
        DateOnly start = NextFreeDate();

        (HttpClient guest, _, Guid requestId) = await BookAsGuestAsync(listingId, start, 2, "We are two quiet hikers.");
        (_, _, Guid otherRequestId) = await BookAsGuestAsync(listingId, start.AddDays(10), 2);

        BookingResponse request = await GetBookingAsync(guest, requestId);
        request.Status.Should().Be(PendingApproval);
        request.ListingHostId.Should().Be(hostId);
        request.MessageToHost.Should().Be("We are two quiet hikers.");

        // The pending request holds the dates.
        (HttpClient someoneElse, Guid someoneElseId) = await CreateAuthenticatedUserAsync();
        await ShouldFailWithAsync(
            await PostBookingAsync(someoneElse, someoneElseId, listingId, start, 2),
            HttpStatusCode.Conflict,
            "Booking.Overlap");

        PagedList<BookingResponse>? pending = await host.GetFromJsonAsync<PagedList<BookingResponse>>(
            $"api/v1/hosting/bookings?status={PendingApproval}&listingId={listingId}");
        pending!.Items.Select(b => b.Id).Should().BeEquivalentTo([requestId, otherRequestId]);
        pending.Items.Should().OnlyContain(b => b.CheckInInstructions == "The key is under the mat.");

        HostingSummaryResponse? summary = await host.GetFromJsonAsync<HostingSummaryResponse>("api/v1/hosting/summary");
        summary!.Should().BeEquivalentTo(new { ListingCount = 1, PendingRequests = 2, UpcomingStays = 0, Currency = "USD" });

        // Only the host answers.
        await ShouldFailWithAsync(await guest.PutAsync($"api/v1/hosting/bookings/{requestId}/accept", null), HttpStatusCode.Forbidden, "Apartment.NotOwner");

        (await host.PutAsync($"api/v1/hosting/bookings/{requestId}/accept", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await host.PutAsync($"api/v1/hosting/bookings/{otherRequestId}/decline", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await GetBookingAsync(guest, requestId)).Status.Should().Be(Reserved);
        (await GetBookingAsync(host, otherRequestId)).Status.Should().Be(Rejected);
        await ShouldFailWithAsync(
            await host.PutAsync($"api/v1/hosting/bookings/{requestId}/decline", null),
            HttpStatusCode.BadRequest,
            "Booking.NotPendingApproval");

        (await host.GetFromJsonAsync<HostingSummaryResponse>("api/v1/hosting/summary"))!
            .Should().BeEquivalentTo(new { PendingRequests = 0, UpcomingStays = 1 });
    }

    [Fact]
    public async Task Guest_Should_BeAbleToCancelAPendingRequest()
    {
        (_, _, Guid listingId) = await CreateHostWithListingAsync(listing => listing with { InstantBook = false });
        (HttpClient guest, Guid guestId, Guid requestId) = await BookAsGuestAsync(listingId, NextFreeDate(), 2);

        (await guest.PutAsJsonAsync($"api/v1/bookings/{requestId}/cancel", new CancelBookingRequest(guestId)))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await GetBookingAsync(guest, requestId)).Status.Should().Be(Cancelled);
    }

    [Fact]
    public async Task ExpiryJob_Should_ExpireRequestsOlderThan24Hours()
    {
        (HttpClient host, _, Guid listingId) = await CreateHostWithListingAsync(listing => listing with { InstantBook = false });
        (_, _, Guid oldRequestId) = await BookAsGuestAsync(listingId, NextFreeDate(), 2);
        (_, _, Guid freshRequestId) = await BookAsGuestAsync(listingId, NextFreeDate(), 2);

        await ExecuteSqlAsync($"UPDATE bookings SET created_on_utc = now() - interval '25 hours' WHERE id = {oldRequestId}");

        await using (AsyncServiceScope scope = Factory.Services.CreateAsyncScope())
        {
            (await scope.ServiceProvider.GetRequiredService<IExpireBookingRequestsJob>().ExpireAsync())
                .Should().BeGreaterThanOrEqualTo(1);
        }

        (await GetBookingAsync(host, oldRequestId)).Status.Should().Be(Expired);
        (await GetBookingAsync(host, freshRequestId)).Status.Should().Be(PendingApproval);

        // Expired requests no longer hold the dates.
        await ShouldFailWithAsync(
            await host.PutAsync($"api/v1/hosting/bookings/{oldRequestId}/accept", null),
            HttpStatusCode.BadRequest,
            "Booking.NotPendingApproval");
    }

    [Fact]
    public async Task CheckInInstructions_Should_OnlyBeShownToTheHost_AndToTheGuestOnceConfirmed()
    {
        (HttpClient host, _, Guid listingId) = await CreateHostWithListingAsync();
        (HttpClient guest, _, Guid bookingId) = await BookAsGuestAsync(listingId, NextFreeDate(), 2);

        (await GetBookingAsync(guest, bookingId)).CheckInInstructions.Should().BeNull();
        (await GetBookingAsync(host, bookingId)).CheckInInstructions.Should().Be("The key is under the mat.");

        (HttpClient stranger, _) = await CreateAuthenticatedUserAsync();
        (await stranger.GetAsync($"api/v1/bookings/{bookingId}")).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Paid (what the Stripe webhook does).
        await UpdateBookingAsync(bookingId, booking => booking.Confirm(DateTime.UtcNow));

        BookingResponse confirmed = await GetBookingAsync(guest, bookingId);
        confirmed.Status.Should().Be(Confirmed);
        confirmed.CheckInInstructions.Should().Be("The key is under the mat.");
    }

    [Fact]
    public async Task Guest_Should_ChangeTheDatesOfAPendingOrReservedBooking()
    {
        (_, _, Guid listingId) = await CreateHostWithListingAsync(listing => listing with
        {
            Price = 100,
            CleaningFee = 0,
            Amenities = [],
            MinNights = 2,
            MaxNights = 10,
        });
        DateOnly start = NextFreeDate();
        (HttpClient guest, _, Guid bookingId) = await BookAsGuestAsync(listingId, start, 2);
        (_, _, Guid otherBookingId) = await BookAsGuestAsync(listingId, start.AddDays(10), 2);

        // Overlapping itself is fine (the booking is ignored), the stay gets longer and the price follows.
        HttpResponseMessage moved = await guest.PutAsJsonAsync(
            $"api/v1/bookings/{bookingId}/dates",
            new ChangeBookingDatesRequest(start.AddDays(1), start.AddDays(5)));
        moved.StatusCode.Should().Be(HttpStatusCode.NoContent, await moved.Content.ReadAsStringAsync());

        BookingResponse booking = await GetBookingAsync(guest, bookingId);
        DateOnly.FromDateTime(booking.DurationStart).Should().Be(start.AddDays(1));
        DateOnly.FromDateTime(booking.DurationEnd).Should().Be(start.AddDays(5));
        booking.TotalPriceAmount.Should().Be(400);

        await ShouldFailWithAsync(
            await guest.PutAsJsonAsync($"api/v1/bookings/{bookingId}/dates", new ChangeBookingDatesRequest(start.AddDays(9), start.AddDays(12))),
            HttpStatusCode.Conflict,
            "Booking.Overlap");
        await ShouldFailWithAsync(
            await guest.PutAsJsonAsync($"api/v1/bookings/{bookingId}/dates", new ChangeBookingDatesRequest(start, start.AddDays(1))),
            HttpStatusCode.BadRequest,
            "Booking.StayTooShort");

        (HttpClient stranger, _) = await CreateAuthenticatedUserAsync();
        await ShouldFailWithAsync(
            await stranger.PutAsJsonAsync($"api/v1/bookings/{otherBookingId}/dates", new ChangeBookingDatesRequest(start, start.AddDays(3))),
            HttpStatusCode.Forbidden,
            "User.InvalidPermissions");

        await UpdateBookingAsync(bookingId, b => b.Confirm(DateTime.UtcNow));
        await ShouldFailWithAsync(
            await guest.PutAsJsonAsync($"api/v1/bookings/{bookingId}/dates", new ChangeBookingDatesRequest(start.AddDays(20), start.AddDays(23))),
            HttpStatusCode.Conflict,
            "Booking.ChangeRequiresCancellation");
    }

    [Fact]
    public async Task Guests_Should_NotBookTheirOwnListing()
    {
        (HttpClient host, Guid hostId, Guid listingId) = await CreateHostWithListingAsync();

        await ShouldFailWithAsync(
            await PostBookingAsync(host, hostId, listingId, NextFreeDate(), 2),
            HttpStatusCode.BadRequest,
            "Booking.OwnListing");
    }
}
