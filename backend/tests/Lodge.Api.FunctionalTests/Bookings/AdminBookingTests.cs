using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Lodge.Api.FunctionalTests.Abstractions;
using Lodge.Api.FunctionalTests.Extensions;
using Lodge.Contracts.Apartments;
using Lodge.Contracts.Authentication;
using Lodge.Contracts.Bookings;
using Lodge.Contracts.Common;
using Lodge.Contracts.Users;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Lodge.Api.FunctionalTests.Bookings;

public class AdminBookingTests(FunctionalTestWebAppFactory factory) : BaseFunctionalTest(factory)
{
    // Far in the future and unique per test so seeded and other tests' bookings never overlap.
    private static int _offset = 3000;

    [Theory]
    [InlineData(DemoEmail, DemoPassword, "Guest")]
    [InlineData(AdminEmail, AdminPassword, "Admin")]
    public async Task Login_Should_IssueTheRoleClaim_AndAuthShouldReturnTheRole(string email, string password, string role)
    {
        HttpResponseMessage response = await HttpClient.PostAsJsonAsync(
            "api/v1/authentication/login",
            new LoginRequest(email, password));
        response.EnsureSuccessStatusCode();
        TokenResponse token = await response.GetTokenAsync();

        new JsonWebToken(token.Token).GetClaim("role").Value.Should().Be(role);

        (HttpClient client, _) = Authenticate(token);
        UserResponse? user = await client.GetFromJsonAsync<UserResponse>("api/v1/users/auth");

        user!.Role.Should().Be(role);
        user.Email.Should().Be(email);
    }

    [Fact]
    public async Task Guest_Should_GetForbidden_OnAdminEndpoints()
    {
        (HttpClient client, _) = await LoginAsDemoUserAsync();
        Guid bookingId = Guid.NewGuid();

        (await client.PutAsync($"api/v1/bookings/{bookingId}/reject", null)).StatusCode
            .Should().Be(HttpStatusCode.Forbidden);
        (await client.PutAsync($"api/v1/bookings/{bookingId}/complete", null)).StatusCode
            .Should().Be(HttpStatusCode.Forbidden);
        (await client.GetAsync("api/v1/bookings")).StatusCode
            .Should().Be(HttpStatusCode.Forbidden);
        (await client.GetAsync("api/v1/bookings?status=1")).StatusCode
            .Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Anonymous_Should_GetUnauthorized_OnAdminEndpoints()
    {
        (await HttpClient.GetAsync("api/v1/bookings")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await HttpClient.PutAsync($"api/v1/bookings/{Guid.NewGuid()}/reject", null)).StatusCode
            .Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Admin_Should_RejectAnotherUsersReservedBooking()
    {
        (HttpClient guestClient, Guid guestId, string guestEmail) = await CreateGuestAsync();
        Guid bookingId = await ReserveAsync(guestClient, guestId);
        (HttpClient adminClient, _) = await LoginAsAdminAsync();

        // No request body at all.
        HttpResponseMessage response = await adminClient.PutAsync($"api/v1/bookings/{bookingId}/reject", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        BookingResponse? booking = await guestClient.GetFromJsonAsync<BookingResponse>($"api/v1/bookings/{bookingId}");
        booking!.Status.Should().Be(3);
        booking.GuestEmail.Should().Be(guestEmail);
        booking.GuestName.Should().Be("First Last");
        booking.HasReview.Should().BeFalse();

        // Rejecting twice violates the status rules (only reserved bookings can be rejected).
        (await adminClient.PutAsync($"api/v1/bookings/{bookingId}/reject", JsonContent.Create(new { })))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Admin_Should_NotCompleteABookingThatIsNotConfirmed()
    {
        (HttpClient guestClient, Guid guestId, _) = await CreateGuestAsync();
        Guid bookingId = await ReserveAsync(guestClient, guestId);
        (HttpClient adminClient, _) = await LoginAsAdminAsync();

        HttpResponseMessage response = await adminClient.PutAsync($"api/v1/bookings/{bookingId}/complete", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.GetProblemDetailsAsync()).Title.Should().Be("Booking.NotConfirmed");
    }

    [Fact]
    public async Task Admin_Should_ListAllBookings_FilteredByStatus_NewestFirst()
    {
        (HttpClient guestClient, Guid guestId, string guestEmail) = await CreateGuestAsync();
        Guid bookingId = await ReserveAsync(guestClient, guestId);
        (HttpClient adminClient, _) = await LoginAsAdminAsync();

        PagedList<BookingResponse>? reserved = await adminClient.GetFromJsonAsync<PagedList<BookingResponse>>(
            "api/v1/bookings?status=1&page=1&pageSize=100");

        reserved!.Items.Should().NotBeEmpty();
        reserved.Items.Should().OnlyContain(b => b.Status == 1);
        reserved.Items.Should().BeInDescendingOrder(b => b.CreatedOnUtc);
        reserved.Items.Should().ContainSingle(b => b.Id == bookingId)
            .Which.GuestEmail.Should().Be(guestEmail);

        // Spans users: the seeded demo user's bookings are included too.
        PagedList<BookingResponse>? all = await adminClient.GetFromJsonAsync<PagedList<BookingResponse>>(
            "api/v1/bookings?pageSize=100");
        all!.TotalCount.Should().BeGreaterThan(reserved.TotalCount);
        all.Items.Select(b => b.GuestEmail).Should().Contain([DemoEmail, guestEmail]);

        PagedList<BookingResponse>? firstPage = await adminClient.GetFromJsonAsync<PagedList<BookingResponse>>(
            "api/v1/bookings");
        firstPage!.Page.Should().Be(1);
        firstPage.PageSize.Should().Be(10);
        firstPage.Items.Should().HaveCountLessThanOrEqualTo(10);
    }

    [Theory]
    [InlineData("status=0")]
    [InlineData("status=8")]
    [InlineData("pageSize=101")]
    [InlineData("page=0")]
    public async Task Admin_List_Should_ReturnBadRequest_ForInvalidParameters(string queryString)
    {
        (HttpClient adminClient, _) = await LoginAsAdminAsync();

        (await adminClient.GetAsync($"api/v1/bookings?{queryString}")).StatusCode
            .Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task BookingJson_Should_UseTheContractFieldNames()
    {
        (HttpClient adminClient, _) = await LoginAsAdminAsync();

        using JsonDocument document = JsonDocument.Parse(await adminClient.GetStringAsync("api/v1/bookings?pageSize=1"));
        JsonElement booking = document.RootElement.GetProperty("items")[0];

        booking.GetProperty("guestName").ValueKind.Should().Be(JsonValueKind.String);
        booking.GetProperty("guestEmail").ValueKind.Should().Be(JsonValueKind.String);
        booking.GetProperty("hasReview").ValueKind.Should().BeOneOf(JsonValueKind.True, JsonValueKind.False);
        booking.GetProperty("status").ValueKind.Should().Be(JsonValueKind.Number);
    }

    private async Task<(HttpClient Client, Guid UserId, string Email)> CreateGuestAsync()
    {
        string email = $"{Guid.NewGuid():N}@lodge.test";
        HttpResponseMessage response = await HttpClient.PostAsJsonAsync(
            "api/v1/authentication/register",
            new RegisterRequest("First", "Last", email, Password));
        response.EnsureSuccessStatusCode();

        (HttpClient client, Guid userId) = Authenticate(await response.GetTokenAsync());

        await VerifyEmailAsync(email);

        return (client, userId, email);
    }

    private async Task<Guid> ReserveAsync(HttpClient client, Guid userId)
    {
        int offset = Interlocked.Add(ref _offset, 10);
        DateOnly start = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(offset);

        HttpResponseMessage response = await PostBookingAsync(client, userId, await GetInstantBookApartmentIdAsync(), start, 3);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<Guid>();
    }
}
