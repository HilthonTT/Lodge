using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Lodge.Application.Abstractions.App;
using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Core.Errors;
using Lodge.Contracts.Authentication;
using Lodge.Domain.Core.Primitives;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lodge.Infrastructure.Authentication.External;

/// <summary>
/// The API-driven OAuth 2.0 authorization-code flow:
/// <list type="bullet">
/// <item>the <c>state</c> parameter is a random nonce that must match the one in the HTTP-only correlation cookie
/// (CSRF protection: the callback only succeeds in the browser that started the login);</item>
/// <item>the cookie value is encrypted and time-limited (10 minutes) with Data Protection and also carries the
/// PKCE code verifier and the return URL.</item>
/// </list>
/// </summary>
internal sealed partial class ExternalAuthenticationService(
    IEnumerable<IExternalLoginProvider> providers,
    IOptions<ExternalAuthenticationOptions> externalOptions,
    IOptions<AppOptions> appOptions,
    IDataProtectionProvider dataProtectionProvider,
    IHttpClientFactory httpClientFactory,
    ILogger<ExternalAuthenticationService> logger) : IExternalAuthenticationService
{
    public const string HttpClientName = "external-login";

    internal static readonly TimeSpan CorrelationLifetime = TimeSpan.FromMinutes(10);

    private readonly ITimeLimitedDataProtector _protector = dataProtectionProvider
        .CreateProtector("Lodge.ExternalLogin.Correlation.v1")
        .ToTimeLimitedDataProtector();

    public IReadOnlyList<ExternalProviderResponse> GetProviders() =>
        providers
            .Where(provider => provider.GetOptions(externalOptions.Value).IsConfigured)
            .Select(provider => new ExternalProviderResponse(provider.Name, provider.DisplayName))
            .ToList();

    public Result<ExternalLoginChallenge> CreateChallenge(string provider, string returnUrl)
    {
        if (FindConfigured(provider) is not { } loginProvider)
        {
            return Result.Failure<ExternalLoginChallenge>(ExternalAuthenticationErrors.ProviderNotConfigured);
        }

        string nonce = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));
        string codeVerifier = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));
        string codeChallenge = Base64Url.EncodeToString(SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier)));

        var correlation = new Correlation(loginProvider.Name, nonce, codeVerifier, returnUrl);
        string protectedCorrelation = _protector.Protect(JsonSerializer.Serialize(correlation), CorrelationLifetime);

        string authorizationUrl = loginProvider.BuildAuthorizationUrl(
            loginProvider.GetOptions(externalOptions.Value),
            RedirectUri(loginProvider),
            nonce,
            codeChallenge);

        return new ExternalLoginChallenge(authorizationUrl, protectedCorrelation);
    }

    public async Task<Result<ExternalLoginCallback>> CompleteAsync(
        string provider,
        string? code,
        string? state,
        string? correlation,
        CancellationToken cancellationToken = default)
    {
        if (FindConfigured(provider) is not { } loginProvider)
        {
            return Result.Failure<ExternalLoginCallback>(ExternalAuthenticationErrors.ProviderNotConfigured);
        }

        Correlation? expected = ReadCorrelation(correlation);

        if (expected is null ||
            expected.Provider != loginProvider.Name ||
            string.IsNullOrEmpty(state) ||
            !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(state), Encoding.UTF8.GetBytes(expected.Nonce)))
        {
            return Result.Failure<ExternalLoginCallback>(ExternalAuthenticationErrors.InvalidState);
        }

        if (string.IsNullOrEmpty(code))
        {
            return Result.Failure<ExternalLoginCallback>(ExternalAuthenticationErrors.ProviderError);
        }

        ExternalIdentity? identity;

        try
        {
            identity = await loginProvider.GetIdentityAsync(
                httpClientFactory.CreateClient(HttpClientName),
                loginProvider.GetOptions(externalOptions.Value),
                code,
                RedirectUri(loginProvider),
                expected.CodeVerifier,
                cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or TaskCanceledException)
        {
            LogProviderFailed(logger, exception, loginProvider.Name);
            identity = null;
        }

        return identity is null
            ? Result.Failure<ExternalLoginCallback>(ExternalAuthenticationErrors.ProviderError)
            : new ExternalLoginCallback(identity, expected.ReturnUrl);
    }

    private IExternalLoginProvider? FindConfigured(string provider) =>
        providers.FirstOrDefault(p =>
            string.Equals(p.Name, provider, StringComparison.OrdinalIgnoreCase) &&
            p.GetOptions(externalOptions.Value).IsConfigured);

    private string RedirectUri(IExternalLoginProvider provider) =>
        appOptions.Value.ApiUrl($"api/v1/authentication/external/{provider.Name}/callback");

    private Correlation? ReadCorrelation(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<Correlation>(_protector.Unprotect(value));
        }
        catch (Exception exception) when (exception is CryptographicException or JsonException)
        {
            return null;
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "The external login provider {Provider} failed")]
    private static partial void LogProviderFailed(ILogger logger, Exception exception, string provider);

    private sealed record Correlation(string Provider, string Nonce, string CodeVerifier, string ReturnUrl);
}
