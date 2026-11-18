using System.Net.Http.Headers;
using System.Net.Http.Json;
using Lodge.Api.FunctionalTests.Extensions;
using Lodge.Contracts.Apartments;
using Lodge.Contracts.Authentication;
using Lodge.Contracts.Bookings;
using Lodge.Contracts.Common;
using Lodge.Contracts.Emails;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Lodge.Api.FunctionalTests.Abstractions;

[Collection(nameof(FunctionalTestCollection))]
public abstract class BaseFunctionalTest(FunctionalTestWebAppFactory factory)
{
    protected const string DemoEmail = "demo@lodge.dev";
    protected const string DemoPassword = "Demo123!";
    protected const string AdminEmail = "admin@lodge.dev";
    protected const string AdminPassword = "Admin123!";
    protected const string Password = "AbC-123";
    protected const string HostEmail = "host@lodge.dev";
    protected const string HostPassword = "Host123!";
    protected const string CanalHouseboat = "Canal Houseboat";

    protected HttpClient HttpClient { get; } = factory.CreateClient();

    protected FunctionalTestWebAppFactory Factory { get; } = factory;

    /// <summary>
    /// Registers a new user and returns an authenticated client plus the user's id. By default the e-mail address
    /// is verified through the e-mailed link (bookings require a verified e-mail).
    /// </summary>
    protected async Task<(HttpClient Client, Guid UserId)> CreateAuthenticatedUserAsync(bool verifyEmail = true)
    {
        (HttpClient client, Guid userId, _) = await RegisterAsync(verifyEmail);

        return (client, userId);
    }

    /// <summary>
    /// Registers a new user; also returns the e-mail address.
    /// </summary>
    protected async Task<(HttpClient Client, Guid UserId, string Email)> RegisterAsync(bool verifyEmail = true)
    {
        string email = $"{Guid.NewGuid():N}@lodge.test";
        var request = new RegisterRequest("First", "Last", email, Password);

        HttpResponseMessage response = await HttpClient.PostAsJsonAsync("api/v1/authentication/register", request);
        response.EnsureSuccessStatusCode();

        (HttpClient client, Guid userId) = Authenticate(await response.GetTokenAsync());

        if (verifyEmail)
        {
            await VerifyEmailAsync(email);
        }

        return (client, userId, email);
    }

    /// <summary>
    /// Verifies the e-mail address with the token of the latest verification e-mail.
    /// </summary>
    protected async Task VerifyEmailAsync(string email)
    {
        EmailVerificationEmail verificationEmail = Factory.Emails.VerificationEmails(email)[^1];

        HttpResponseMessage response = await HttpClient.PostAsJsonAsync(
            "api/v1/authentication/verify-email",
            new VerifyEmailRequest(RecordingEmailNotificationService.TokenFromLink(verificationEmail.Link)));
        response.EnsureSuccessStatusCode();
    }

    protected Task<(HttpClient Client, Guid UserId)> LoginAsDemoUserAsync() => LoginAsync(DemoEmail, DemoPassword);

    protected Task<(HttpClient Client, Guid UserId)> LoginAsAdminAsync() => LoginAsync(AdminEmail, AdminPassword);

    protected async Task<(HttpClient Client, Guid UserId)> LoginAsync(string email, string password)
    {
        HttpResponseMessage response = await HttpClient.PostAsJsonAsync(
            "api/v1/authentication/login",
            new LoginRequest(email, password));
        response.EnsureSuccessStatusCode();

        return Authenticate(await response.GetTokenAsync());
    }

    protected Task<(HttpClient Client, Guid UserId)> LoginAsHostAsync() => LoginAsync(HostEmail, HostPassword);

    /// <summary>
    /// Finds a seeded, listed apartment by its exact name.
    /// </summary>
    protected async Task<ApartmentResponse> FindApartmentAsync(string name)
    {
        PagedList<ApartmentResponse>? page = await HttpClient.GetFromJsonAsync<PagedList<ApartmentResponse>>(
            $"api/v1/apartments/search?searchTerm={Uri.EscapeDataString(name)}&pageSize=100");

        return page!.Items.Single(apartment => apartment.Name == name);
    }

    /// <summary>
    /// The id of a seeded instant-book listing with a one-night minimum (the Canal Houseboat, for 2 guests).
    /// </summary>
    protected async Task<Guid> GetInstantBookApartmentIdAsync() => (await FindApartmentAsync(CanalHouseboat)).Id;

    /// <summary>
    /// Posts a booking (with a fresh idempotency key).
    /// </summary>
    protected static async Task<HttpResponseMessage> PostBookingAsync(
        HttpClient client,
        Guid userId,
        Guid apartmentId,
        DateOnly start,
        int nights,
        int guestCount = 1,
        string? messageToHost = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/bookings")
        {
            Content = JsonContent.Create(new ReserveBookingRequest(
                apartmentId,
                userId,
                start,
                start.AddDays(nights),
                guestCount,
                messageToHost)),
        };
        request.Headers.Add("X-Idempotency-Key", Guid.NewGuid().ToString());

        return await client.SendAsync(request);
    }

    protected (HttpClient Client, Guid UserId) Authenticate(TokenResponse token) => Authenticate(token.Token);

    protected (HttpClient Client, Guid UserId) Authenticate(string token)
    {
        HttpClient client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        Guid userId = Guid.Parse(new JsonWebToken(token).GetClaim("userId").Value);

        return (client, userId);
    }
}
