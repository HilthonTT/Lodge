using System.Net;
using System.Net.Http.Json;
using Lodge.Api.FunctionalTests.Abstractions;
using Lodge.Contracts.Authentication;
using Lodge.Contracts.Bookings;
using Lodge.Contracts.Stripe;

namespace Lodge.Api.FunctionalTests.Bookings;

public class BookingTests(FunctionalTestWebAppFactory factory) : BaseFunctionalTest(factory)
{
    [Fact]
    public async Task DemoUser_Should_SeeTheirSeededBookings()
    {
        (HttpClient client, Guid userId) = await LoginAsDemoUserAsync();

        List<BookingResponse>? bookings = await client.GetFromJsonAsync<List<BookingResponse>>(
            $"api/v1/users/{userId}/bookings");

        bookings.Should().NotBeNull();
        bookings!.Select(b => b.Status).Should().Contain([1, 2, 3, 4, 5]);
        bookings.Should().OnlyContain(b => b.CleaningFeeCurrency == "USD" && b.TotalPriceAmount > 0);
    }

    [Fact]
    public async Task GetById_Should_NotLeakBookings_ToOtherUsers()
    {
        // Regression: bookings were cached by id, so a cache hit skipped the ownership check.
        (HttpClient demoClient, Guid demoUserId) = await LoginAsDemoUserAsync();
        List<BookingResponse>? bookings = await demoClient.GetFromJsonAsync<List<BookingResponse>>(
            $"api/v1/users/{demoUserId}/bookings");
        Guid bookingId = bookings![0].Id;

        (await demoClient.GetAsync($"api/v1/bookings/{bookingId}")).StatusCode.Should().Be(HttpStatusCode.OK);

        (HttpClient otherClient, _) = await CreateAuthenticatedUserAsync();
        HttpResponseMessage response = await otherClient.GetAsync($"api/v1/bookings/{bookingId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetBookingsByUserId_Should_ReturnForbidden_ForAnotherUser()
    {
        (HttpClient client, _) = await CreateAuthenticatedUserAsync();
        (_, Guid otherUserId) = await CreateAuthenticatedUserAsync();

        HttpResponseMessage response = await client.GetAsync($"api/v1/users/{otherUserId}/bookings");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Login_Should_ReturnBadRequest_ForWrongPassword()
    {
        // Regression: "Problem" errors were mapped to 500 Internal Server Error.
        HttpResponseMessage response = await HttpClient.PostAsJsonAsync(
            "api/v1/authentication/login",
            new LoginRequest(DemoEmail, "Wrong-Passw0rd"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Checkout_Should_ReturnNotFound_ForAnUnknownBooking()
    {
        (HttpClient client, Guid userId) = await LoginAsDemoUserAsync();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "api/v1/stripe/checkout",
            new StripeCheckoutRequest(userId, Guid.NewGuid()));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Health_Live_Should_ReturnOk()
    {
        HttpResponseMessage response = await HttpClient.GetAsync("health/live");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
