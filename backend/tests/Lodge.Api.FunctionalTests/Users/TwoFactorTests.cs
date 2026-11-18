using System.Net;
using System.Net.Http.Json;
using Lodge.Api.FunctionalTests.Abstractions;
using Lodge.Api.FunctionalTests.Extensions;
using Lodge.Contracts.Authentication;
using Lodge.Contracts.Users;
using Lodge.Infrastructure.Cryptography;

namespace Lodge.Api.FunctionalTests.Users;

public class TwoFactorTests(FunctionalTestWebAppFactory factory) : BaseFunctionalTest(factory)
{
    // The time step when the test starts; the server accepts codes of one step before and after.
    private readonly long _step = TotpProvider.GetTimeStep(DateTime.UtcNow);

    [Fact]
    public async Task Enable_Should_RequireAValidCode_AndLoginShouldThenRequireASecondFactor()
    {
        (HttpClient client, Guid userId, string email) = await RegisterAsync();

        TwoFactorSetupResponse setup = await SetupAsync(client, userId);
        setup.OtpauthUri.Should().StartWith("otpauth://totp/Lodge%3A").And.Contain($"secret={setup.Secret}");
        (await client.GetFromJsonAsync<UserResponse>("api/v1/users/auth"))!.TwoFactorEnabled.Should().BeFalse();

        HttpResponseMessage wrongCode = await client.PostAsJsonAsync(
            $"api/v1/users/{userId}/two-factor/enable", new TwoFactorCodeRequest(WrongCode(setup.Secret)));
        wrongCode.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await wrongCode.GetProblemDetailsAsync()).Title.Should().Be("TwoFactor.InvalidCode");

        RecoveryCodesResponse recovery = await EnableAsync(client, userId, setup.Secret);
        recovery.RecoveryCodes.Should().HaveCount(10).And.OnlyHaveUniqueItems();

        (await client.GetFromJsonAsync<UserResponse>("api/v1/users/auth"))!.TwoFactorEnabled.Should().BeTrue();
        (await client.PostAsync($"api/v1/users/{userId}/two-factor/setup", null)).StatusCode
            .Should().Be(HttpStatusCode.Conflict);

        // Password login now returns a challenge instead of an access token.
        LoginResponse challenge = await LoginAsync(email);
        challenge.RequiresTwoFactor.Should().BeTrue();
        challenge.Token.Should().BeNull();
        challenge.TwoFactorToken.Should().NotBeNullOrEmpty();

        // The challenge is not an access token.
        HttpClient challengeClient = Factory.CreateClient();
        challengeClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", challenge.TwoFactorToken);
        (await challengeClient.GetAsync("api/v1/users/auth")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        HttpResponseMessage wrong = await LoginTwoFactorAsync(new LoginTwoFactorRequest(
            challenge.TwoFactorToken!, WrongCode(setup.Secret), null));
        wrong.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await wrong.GetProblemDetailsAsync()).Title.Should().Be("Authentication.InvalidTwoFactorCode");

        HttpResponseMessage missing = await LoginTwoFactorAsync(new LoginTwoFactorRequest(challenge.TwoFactorToken!, null, null));
        missing.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // A recovery code works once.
        HttpResponseMessage withRecoveryCode = await LoginTwoFactorAsync(new LoginTwoFactorRequest(
            challenge.TwoFactorToken!, null, recovery.RecoveryCodes[0].ToUpperInvariant()));
        withRecoveryCode.StatusCode.Should().Be(HttpStatusCode.OK);
        (HttpClient signedIn, _) = Authenticate(await withRecoveryCode.GetTokenAsync());
        (await signedIn.GetAsync("api/v1/users/auth")).StatusCode.Should().Be(HttpStatusCode.OK);

        LoginResponse secondChallenge = await LoginAsync(email);
        (await LoginTwoFactorAsync(new LoginTwoFactorRequest(secondChallenge.TwoFactorToken!, null, recovery.RecoveryCodes[0])))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // A TOTP code of a later step than the one used to enable 2FA.
        HttpResponseMessage withCode = await LoginTwoFactorAsync(new LoginTwoFactorRequest(
            secondChallenge.TwoFactorToken!, Code(setup.Secret, _step + 1), null));
        withCode.StatusCode.Should().Be(HttpStatusCode.OK);

        // ...which cannot be replayed.
        LoginResponse thirdChallenge = await LoginAsync(email);
        (await LoginTwoFactorAsync(new LoginTwoFactorRequest(thirdChallenge.TwoFactorToken!, Code(setup.Secret, _step + 1), null)))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ChallengeToken_Should_BeRejected_AfterTooManyWrongCodes()
    {
        (HttpClient client, Guid userId, string email) = await RegisterAsync();
        TwoFactorSetupResponse setup = await SetupAsync(client, userId);
        await EnableAsync(client, userId, setup.Secret);

        LoginResponse challenge = await LoginAsync(email);

        for (int i = 0; i < 5; i++)
        {
            await LoginTwoFactorAsync(new LoginTwoFactorRequest(challenge.TwoFactorToken!, WrongCode(setup.Secret), null));
        }

        HttpResponseMessage response = await LoginTwoFactorAsync(new LoginTwoFactorRequest(
            challenge.TwoFactorToken!, Code(setup.Secret, _step + 1), null));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.GetProblemDetailsAsync()).Title.Should().Be("Authentication.InvalidTwoFactorToken");
    }

    [Fact]
    public async Task Disable_Should_RequireThePasswordAndACode()
    {
        (HttpClient client, Guid userId, string email) = await RegisterAsync();
        TwoFactorSetupResponse setup = await SetupAsync(client, userId);
        await EnableAsync(client, userId, setup.Secret);

        HttpResponseMessage wrongPassword = await client.PostAsJsonAsync(
            $"api/v1/users/{userId}/two-factor/disable",
            new DisableTwoFactorRequest("Wr0ng-Password", Code(setup.Secret, _step + 1)));
        wrongPassword.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await wrongPassword.GetProblemDetailsAsync()).Title.Should().Be("TwoFactor.InvalidPassword");

        HttpResponseMessage disabled = await client.PostAsJsonAsync(
            $"api/v1/users/{userId}/two-factor/disable",
            new DisableTwoFactorRequest(Password, Code(setup.Secret, _step + 1)));
        disabled.StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await client.GetFromJsonAsync<UserResponse>("api/v1/users/auth"))!.TwoFactorEnabled.Should().BeFalse();
        LoginResponse login = await LoginAsync(email);
        login.RequiresTwoFactor.Should().BeFalse();
        login.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RecoveryCodes_Should_BeRegenerated_AndOldOnesInvalidated()
    {
        (HttpClient client, Guid userId, string email) = await RegisterAsync();
        TwoFactorSetupResponse setup = await SetupAsync(client, userId);
        RecoveryCodesResponse original = await EnableAsync(client, userId, setup.Secret);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"api/v1/users/{userId}/two-factor/recovery-codes",
            new TwoFactorCodeRequest(Code(setup.Secret, _step + 1)));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        RecoveryCodesResponse regenerated = (await response.Content.ReadFromJsonAsync<RecoveryCodesResponse>())!;

        regenerated.RecoveryCodes.Should().HaveCount(10).And.NotIntersectWith(original.RecoveryCodes);

        LoginResponse challenge = await LoginAsync(email);
        (await LoginTwoFactorAsync(new LoginTwoFactorRequest(challenge.TwoFactorToken!, null, original.RecoveryCodes[0])))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await LoginTwoFactorAsync(new LoginTwoFactorRequest(challenge.TwoFactorToken!, null, regenerated.RecoveryCodes[0])))
            .StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task TwoFactorEndpoints_Should_BeForbidden_ForAnotherUser()
    {
        (HttpClient client, _) = await CreateAuthenticatedUserAsync();
        (_, Guid otherUserId) = await CreateAuthenticatedUserAsync();

        (await client.PostAsync($"api/v1/users/{otherUserId}/two-factor/setup", null)).StatusCode
            .Should().Be(HttpStatusCode.Forbidden);
    }

    private static async Task<TwoFactorSetupResponse> SetupAsync(HttpClient client, Guid userId)
    {
        HttpResponseMessage response = await client.PostAsync($"api/v1/users/{userId}/two-factor/setup", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return (await response.Content.ReadFromJsonAsync<TwoFactorSetupResponse>())!;
    }

    private async Task<RecoveryCodesResponse> EnableAsync(HttpClient client, Guid userId, string secret)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"api/v1/users/{userId}/two-factor/enable", new TwoFactorCodeRequest(Code(secret, _step)));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return (await response.Content.ReadFromJsonAsync<RecoveryCodesResponse>())!;
    }

    private async Task<LoginResponse> LoginAsync(string email)
    {
        HttpResponseMessage response = await HttpClient.PostAsJsonAsync(
            "api/v1/authentication/login", new LoginRequest(email, Password));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return (await response.Content.ReadFromJsonAsync<LoginResponse>())!;
    }

    private Task<HttpResponseMessage> LoginTwoFactorAsync(LoginTwoFactorRequest request) =>
        HttpClient.PostAsJsonAsync("api/v1/authentication/login/two-factor", request);

    private static string Code(string secret, long step)
    {
        Base32.TryDecode(secret, out byte[] key).Should().BeTrue();

        return TotpProvider.ComputeCode(key, step, 6);
    }

    // A well-formed code that is valid for none of the accepted steps.
    private string WrongCode(string secret)
    {
        HashSet<string> valid = [.. Enumerable.Range(-2, 5).Select(offset => Code(secret, _step + offset))];

        return Enumerable.Range(0, 1000).Select(i => i.ToString("D6", System.Globalization.CultureInfo.InvariantCulture))
            .First(code => !valid.Contains(code));
    }
}
