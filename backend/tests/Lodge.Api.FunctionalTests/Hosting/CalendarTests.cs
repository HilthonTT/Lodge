using System.Net;
using System.Net.Http.Json;
using Lodge.Api.FunctionalTests.Abstractions;
using Lodge.Contracts.Apartments;
using Lodge.Contracts.Common;
using Lodge.Contracts.Hosting;

namespace Lodge.Api.FunctionalTests.Hosting;

public class CalendarTests(FunctionalTestWebAppFactory factory) : HostingFunctionalTest(factory)
{
    [Fact]
    public async Task BlockedNights_Should_BeUnavailable_LikeBookedNights()
    {
        (HttpClient host, _, Guid listingId) = await CreateHostWithListingAsync();
        DateOnly start = NextFreeDate();
        await BookAsGuestAsync(listingId, start, 2);

        HttpResponseMessage blocked = await host.PutAsJsonAsync(
            $"api/v1/hosting/listings/{listingId}/calendar",
            new UpdateCalendarRequest(start.AddDays(5), start.AddDays(7), Blocked: true));
        blocked.StatusCode.Should().Be(HttpStatusCode.NoContent);

        List<HostCalendarDayResponse>? hostCalendar = await host.GetFromJsonAsync<List<HostCalendarDayResponse>>(
            $"api/v1/hosting/listings/{listingId}/calendar?from={start:yyyy-MM-dd}&to={start.AddDays(9):yyyy-MM-dd}");

        hostCalendar.Should().HaveCount(10);
        hostCalendar![0].Should().BeEquivalentTo(new { Booked = true, Blocked = false, Available = false, Price = 100m });
        hostCalendar[1].Booked.Should().BeTrue();
        hostCalendar[2].Should().BeEquivalentTo(new { Booked = false, Blocked = false, Available = true });
        hostCalendar.Skip(5).Take(3).Should().OnlyContain(day => day.Blocked && !day.Available && !day.Booked);
        hostCalendar[8].Available.Should().BeTrue();

        List<ApartmentCalendarDayResponse>? publicCalendar = await HttpClient.GetFromJsonAsync<List<ApartmentCalendarDayResponse>>(
            $"api/v1/apartments/{listingId}/calendar?from={start:yyyy-MM-dd}&to={start.AddDays(9):yyyy-MM-dd}");

        publicCalendar!.Select(day => day.Available).Should().Equal(false, false, true, true, true, false, false, false, true, true);

        List<DateTime>? unavailable = await HttpClient.GetFromJsonAsync<List<DateTime>>($"api/v1/apartments/{listingId}/booked-dates");
        unavailable!.Select(DateOnly.FromDateTime).Should().Equal(
            start, start.AddDays(1), start.AddDays(5), start.AddDays(6), start.AddDays(7));

        // Reserving over a blocked night fails like an overlap; a stay that checks out on the first blocked night works.
        (HttpClient guest, Guid guestId) = await CreateAuthenticatedUserAsync();
        await ShouldFailWithAsync(
            await PostBookingAsync(guest, guestId, listingId, start.AddDays(4), 2),
            HttpStatusCode.Conflict,
            "Booking.Overlap");
        (await PostBookingAsync(guest, guestId, listingId, start.AddDays(3), 2)).StatusCode.Should().Be(HttpStatusCode.OK);

        // Unblocking removes the block.
        (await host.PutAsJsonAsync(
                $"api/v1/hosting/listings/{listingId}/calendar",
                new UpdateCalendarRequest(start.AddDays(5), start.AddDays(7), Blocked: false)))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await HttpClient.GetFromJsonAsync<List<DateTime>>($"api/v1/apartments/{listingId}/booked-dates"))!
            .Should().HaveCount(4);
    }

    [Fact]
    public async Task PriceOverrides_WeekendPrices_AndDiscounts_Should_ShapeThePrice()
    {
        (HttpClient host, _, Guid listingId) = await CreateHostWithListingAsync(listing => listing with
        {
            Price = 100,
            CleaningFee = 0,
            Amenities = [],
            WeekendPrice = 150,
            WeeklyDiscountPercent = 10,
            MonthlyDiscountPercent = 20,
        });

        // A Monday far enough in the future.
        DateOnly monday = NextFreeDate();
        while (monday.DayOfWeek != DayOfWeek.Monday)
        {
            monday = monday.AddDays(1);
        }

        PriceDetailsResponse? week = await HttpClient.GetFromJsonAsync<PriceDetailsResponse>(
            $"api/v1/apartments/{listingId}/price?startDate={monday:yyyy-MM-dd}&endDate={monday.AddDays(7):yyyy-MM-dd}");

        // Mon-Thu 4 x 100, Fri + Sat 2 x 150, Sun 100 = 800; 10% weekly discount.
        week!.Should().BeEquivalentTo(new
        {
            Currency = "USD",
            Nights = 7,
            PricePerPeriod = 800m,
            DiscountAmount = 80m,
            DiscountType = "weekly",
            AverageNightlyPrice = 114.29m,
            TotalPrice = 720m,
        });

        PriceDetailsResponse? month = await HttpClient.GetFromJsonAsync<PriceDetailsResponse>(
            $"api/v1/apartments/{listingId}/price?startDate={monday:yyyy-MM-dd}&endDate={monday.AddDays(28):yyyy-MM-dd}");
        month!.DiscountType.Should().Be("monthly");
        month.DiscountAmount.Should().Be(month.PricePerPeriod * 0.2m);

        PriceDetailsResponse? twoNights = await HttpClient.GetFromJsonAsync<PriceDetailsResponse>(
            $"api/v1/apartments/{listingId}/price?startDate={monday:yyyy-MM-dd}&endDate={monday.AddDays(2):yyyy-MM-dd}");
        twoNights!.DiscountType.Should().BeNull();
        twoNights.TotalPrice.Should().Be(200);

        // A custom price for Monday and Tuesday wins over the base price.
        (await host.PutAsJsonAsync(
                $"api/v1/hosting/listings/{listingId}/calendar",
                new UpdateCalendarRequest(monday, monday.AddDays(1), Price: 300)))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await HttpClient.GetFromJsonAsync<PriceDetailsResponse>(
                $"api/v1/apartments/{listingId}/price?startDate={monday:yyyy-MM-dd}&endDate={monday.AddDays(2):yyyy-MM-dd}"))!
            .TotalPrice.Should().Be(600);

        List<ApartmentCalendarDayResponse>? calendar = await HttpClient.GetFromJsonAsync<List<ApartmentCalendarDayResponse>>(
            $"api/v1/apartments/{listingId}/calendar?from={monday:yyyy-MM-dd}&to={monday.AddDays(6):yyyy-MM-dd}");
        calendar!.Select(day => day.Price).Should().Equal(300m, 300m, 100m, 100m, 150m, 150m, 100m);

        List<HostCalendarDayResponse>? hostCalendar = await host.GetFromJsonAsync<List<HostCalendarDayResponse>>(
            $"api/v1/hosting/listings/{listingId}/calendar?from={monday:yyyy-MM-dd}&to={monday.AddDays(2):yyyy-MM-dd}");
        hostCalendar!.Select(day => day.CustomPrice).Should().Equal(true, true, false);

        // The booking stores the price with the override.
        (_, _, Guid bookingId) = await BookAsGuestAsync(listingId, monday, 2);
        (await GetBookingAsync(host, bookingId)).TotalPriceAmount.Should().Be(600);

        (await host.PutAsJsonAsync(
                $"api/v1/hosting/listings/{listingId}/calendar",
                new UpdateCalendarRequest(monday, monday.AddDays(1), ClearPrice: true)))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await HttpClient.GetFromJsonAsync<List<ApartmentCalendarDayResponse>>(
                $"api/v1/apartments/{listingId}/calendar?from={monday:yyyy-MM-dd}&to={monday:yyyy-MM-dd}"))!
            .Should().ContainSingle().Which.Price.Should().Be(100);
    }

    [Fact]
    public async Task Booking_Should_RespectMinAndMaxNights()
    {
        (_, _, Guid listingId) = await CreateHostWithListingAsync(listing => listing with { MinNights = 3, MaxNights = 5 });
        (HttpClient guest, Guid guestId) = await CreateAuthenticatedUserAsync();
        DateOnly start = NextFreeDate();

        await ShouldFailWithAsync(await PostBookingAsync(guest, guestId, listingId, start, 2), HttpStatusCode.BadRequest, "Booking.StayTooShort");
        await ShouldFailWithAsync(await PostBookingAsync(guest, guestId, listingId, start, 6), HttpStatusCode.BadRequest, "Booking.StayTooLong");
        (await PostBookingAsync(guest, guestId, listingId, start, 3)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Calendar_Should_ValidateTheRange_AndRefusePastDates()
    {
        (HttpClient host, _, Guid listingId) = await CreateHostWithListingAsync();
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);

        await ShouldFailWithAsync(
            await host.GetAsync($"api/v1/hosting/listings/{listingId}/calendar?from={today:yyyy-MM-dd}&to={today.AddDays(366):yyyy-MM-dd}"),
            HttpStatusCode.BadRequest,
            "Apartment.InvalidCalendarRange");
        await ShouldFailWithAsync(
            await HttpClient.GetAsync($"api/v1/apartments/{listingId}/calendar?from={today:yyyy-MM-dd}&to={today.AddDays(-1):yyyy-MM-dd}"),
            HttpStatusCode.BadRequest,
            "Apartment.InvalidCalendarRange");
        await ShouldFailWithAsync(
            await host.PutAsJsonAsync(
                $"api/v1/hosting/listings/{listingId}/calendar",
                new UpdateCalendarRequest(today.AddDays(-1), today, Blocked: true)),
            HttpStatusCode.BadRequest,
            "Apartment.CalendarInPast");
        await ShouldFailWithAsync(
            await host.PutAsJsonAsync(
                $"api/v1/hosting/listings/{listingId}/calendar",
                new UpdateCalendarRequest(today, today.AddDays(1))),
            HttpStatusCode.BadRequest,
            "Calendar.NothingToChange");

        // Without from/to: 90 days from today.
        (await HttpClient.GetFromJsonAsync<List<ApartmentCalendarDayResponse>>($"api/v1/apartments/{listingId}/calendar"))!
            .Should().HaveCount(90).And.Subject.First().Date.Should().Be(today);
    }
}
