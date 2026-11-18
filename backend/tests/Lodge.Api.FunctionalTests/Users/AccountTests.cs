using System.Net;
using System.Net.Http.Json;
using Lodge.Api.FunctionalTests.Abstractions;
using Lodge.Api.FunctionalTests.Contracts;
using Lodge.Api.FunctionalTests.Extensions;
using Lodge.Contracts.Apartments;
using Lodge.Contracts.Authentication;
using Lodge.Contracts.Bookings;
using Lodge.Contracts.Common;
using Lodge.Contracts.Emails;
using Lodge.Contracts.Users;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Lodge.Api.FunctionalTests.Users;

/// <summary>
/// E-mail verification, the booking gate and password reset.
/// </summary>
public class AccountTests(FunctionalTestWebAppFactory factory) : BaseFunctionalTest(factory)
{
    private const string NewPassword = "N3w-Passw0rd!";

    [Fact]
    public async Task Register_Should_SendAVerificationLink_AndVerifyEmail_ShouldVerifyOnce()
    {
        string email = $"{Guid.NewGuid():N}@lodge.test";
        HttpResponseMessage registered = await HttpClient.PostAsJsonAsync(
            "api/v1/authentication/register",
            new RegisterRequest("First", "Last", email, Password));
        TokenResponse token = await registered.GetTokenAsync();
        (HttpClient client, _) = Authenticate(token);

        new JsonWebToken(token.Token).GetClaim("emailVerified").Value.Should().Be("false");

        UserResponse? before = await client.GetFromJsonAsync<UserResponse>("api/v1/users/auth");
        before!.EmailVerified.Should().BeFalse();
        before.HasPassword.Should().BeTrue();
        before.TwoFactorEnabled.Should().BeFalse();
        before.ExternalLogins.Should().BeEmpty();
        before.AvatarUrl.Should().BeNull();

        EmailVerificationEmail verificationEmail = Factory.Emails.VerificationEmails(email).Should().ContainSingle().Subject;
        verificationEmail.Link.Should().StartWith($"{FunctionalTestWebAppFactory.FrontendBaseUrl}/verify-email?token=");
        string verificationToken = RecordingEmailNotificationService.TokenFromLink(verificationEmail.Link);

        HttpResponseMessage invalid = await HttpClient.PostAsJsonAsync(
            "api/v1/authentication/verify-email", new VerifyEmailRequest("not-a-real-token"));
        HttpResponseMessage verified = await HttpClient.PostAsJsonAsync(
            "api/v1/authentication/verify-email", new VerifyEmailRequest(verificationToken));
        HttpResponseMessage reused = await HttpClient.PostAsJsonAsync(
            "api/v1/authentication/verify-email", new VerifyEmailRequest(verificationToken));

        invalid.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await invalid.GetProblemDetailsAsync()).Title.Should().Be("Authentication.InvalidToken");
        verified.StatusCode.Should().Be(HttpStatusCode.NoContent);
        reused.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await reused.GetProblemDetailsAsync()).Title.Should().Be("Authentication.InvalidToken");

        UserResponse? after = await client.GetFromJsonAsync<UserResponse>("api/v1/users/auth");
        after!.EmailVerified.Should().BeTrue();

        HttpResponseMessage refreshed = await client.PostAsync("api/v1/authentication/refresh-token", null);
        refreshed.StatusCode.Should().Be(HttpStatusCode.OK);
        new JsonWebToken((await refreshed.GetTokenAsync()).Token).GetClaim("emailVerified").Value.Should().Be("true");
    }

    [Fact]
    public async Task Booking_Should_RequireAVerifiedEmail()
    {
        (HttpClient client, Guid userId, string email) = await RegisterAsync(verifyEmail: false);

        HttpResponseMessage forbidden = await ReserveAsync(client, userId);

        forbidden.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await forbidden.GetProblemDetailsAsync()).Title.Should().Be("User.EmailNotVerified");

        await VerifyEmailAsync(email);

        (await ReserveAsync(client, userId)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ResendVerification_Should_BeThrottled_AndRejectedForVerifiedUsers()
    {
        (HttpClient unverifiedClient, _) = await CreateAuthenticatedUserAsync(verifyEmail: false);
        (HttpClient verifiedClient, _) = await CreateAuthenticatedUserAsync();

        // Registration has just sent the first e-mail.
        HttpResponseMessage throttled = await unverifiedClient.PostAsync("api/v1/authentication/resend-verification", null);
        HttpResponseMessage alreadyVerified = await verifiedClient.PostAsync("api/v1/authentication/resend-verification", null);
        HttpResponseMessage anonymous = await HttpClient.PostAsync("api/v1/authentication/resend-verification", null);

        throttled.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        (await throttled.GetProblemDetailsAsync()).Title.Should().Be("User.VerificationEmailThrottled");
        alreadyVerified.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await alreadyVerified.GetProblemDetailsAsync()).Title.Should().Be("User.EmailAlreadyVerified");
        anonymous.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ForgotPassword_Should_AlwaysReturnAccepted()
    {
        string unknownEmail = $"{Guid.NewGuid():N}@lodge.test";

        HttpResponseMessage unknown = await HttpClient.PostAsJsonAsync(
            "api/v1/authentication/forgot-password", new ForgotPasswordRequest(unknownEmail));
        HttpResponseMessage malformed = await HttpClient.PostAsJsonAsync(
            "api/v1/authentication/forgot-password", new ForgotPasswordRequest("not-an-email"));

        unknown.StatusCode.Should().Be(HttpStatusCode.Accepted);
        malformed.StatusCode.Should().Be(HttpStatusCode.Accepted);
        Factory.Emails.PasswordResetEmails(unknownEmail).Should().BeEmpty();
    }

    [Fact]
    public async Task ResetPassword_Should_SetTheNewPassword_AndConsumeTheToken()
    {
        (HttpClient client, _, string email) = await RegisterAsync(verifyEmail: false);

        HttpResponseMessage forgot = await HttpClient.PostAsJsonAsync(
            "api/v1/authentication/forgot-password", new ForgotPasswordRequest(email.ToUpperInvariant()));
        forgot.StatusCode.Should().Be(HttpStatusCode.Accepted);

        PasswordResetEmail resetEmail = Factory.Emails.PasswordResetEmails(email).Should().ContainSingle().Subject;
        resetEmail.Link.Should().StartWith($"{FunctionalTestWebAppFactory.FrontendBaseUrl}/reset-password?token=");
        string token = RecordingEmailNotificationService.TokenFromLink(resetEmail.Link);

        HttpResponseMessage weak = await HttpClient.PostAsJsonAsync(
            "api/v1/authentication/reset-password", new ResetPasswordRequest(token, "weak"));
        HttpResponseMessage reset = await HttpClient.PostAsJsonAsync(
            "api/v1/authentication/reset-password", new ResetPasswordRequest(token, NewPassword));
        HttpResponseMessage reused = await HttpClient.PostAsJsonAsync(
            "api/v1/authentication/reset-password", new ResetPasswordRequest(token, "An0ther-Password"));

        weak.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await weak.GetProblemDetailsAsync()).Title.Should().Be("Password.TooShort");
        reset.StatusCode.Should().Be(HttpStatusCode.NoContent);
        reused.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await reused.GetProblemDetailsAsync()).Title.Should().Be("Authentication.InvalidToken");

        (await HttpClient.PostAsJsonAsync("api/v1/authentication/login", new LoginRequest(email, Password)))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await HttpClient.PostAsJsonAsync("api/v1/authentication/login", new LoginRequest(email, NewPassword)))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        // The link proved ownership of the mailbox.
        (await client.GetFromJsonAsync<UserResponse>("api/v1/users/auth"))!.EmailVerified.Should().BeTrue();
    }

    [Fact]
    public async Task DemoUser_Should_HaveAVerifiedEmail()
    {
        HttpResponseMessage response = await HttpClient.PostAsJsonAsync(
            "api/v1/authentication/login", new LoginRequest(DemoEmail, DemoPassword));

        LoginResponse? login = await response.Content.ReadFromJsonAsync<LoginResponse>();

        login!.RequiresTwoFactor.Should().BeFalse();
        login.TwoFactorToken.Should().BeNull();
        new JsonWebToken(login.Token).GetClaim("emailVerified").Value.Should().Be("true");
    }

    private async Task<HttpResponseMessage> ReserveAsync(HttpClient client, Guid userId)
    {
        DateOnly start = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(4000 + Random.Shared.Next(0, 5000));

        return await PostBookingAsync(client, userId, await GetInstantBookApartmentIdAsync(), start, 2);
    }
}
