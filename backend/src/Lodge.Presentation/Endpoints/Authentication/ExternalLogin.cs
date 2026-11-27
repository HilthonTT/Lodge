using Lodge.Application.Abstractions.App;
using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Authentication.External;
using Lodge.Application.Authentication.External.Challenge;
using Lodge.Application.Authentication.External.Complete;
using Lodge.Domain.Core.Primitives;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace Lodge.Presentation.Endpoints.Authentication;

/// <summary>
/// The browser-facing social login flow:
/// <list type="number">
/// <item><c>GET /authentication/external/{provider}?returnUrl=/path</c> sets an HTTP-only correlation cookie and
/// redirects (302) to the provider;</item>
/// <item>the provider redirects back to <c>GET /authentication/external/{provider}/callback</c>, which redirects to
/// <c>{App:FrontendBaseUrl}/auth/callback#token=...&amp;returnUrl=...</c>
/// (or <c>#twoFactorToken=...&amp;returnUrl=...</c>, or <c>#error=&lt;code&gt;</c>).</item>
/// </list>
/// The result travels in the URL fragment, so it never reaches server logs or the Referer header.
/// </summary>
internal sealed class ExternalLogin : IEndpoint
{
    internal const string CorrelationCookieName = "lodge.external-login";

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("authentication/external/{provider}", async (
            string provider,
            string? returnUrl,
            HttpContext httpContext,
            ISender sender,
            IOptions<AppOptions> appOptions,
            CancellationToken cancellationToken) =>
        {
            Result<ExternalLoginChallenge> result = await sender.Send(
                new ChallengeExternalLoginQuery(provider, returnUrl),
                cancellationToken);

            if (result.IsFailure)
            {
                return Results.Redirect(ErrorUrl(appOptions.Value, result.Error));
            }

            httpContext.Response.Cookies.Append(CorrelationCookieName, result.Value.Correlation, new CookieOptions
            {
                HttpOnly = true,
                // Lax: the cookie must be sent on the top-level redirect back from the provider.
                SameSite = SameSiteMode.Lax,
                Secure = httpContext.Request.IsHttps,
                Path = CookiePath(httpContext),
                MaxAge = TimeSpan.FromMinutes(10),
                IsEssential = true,
            });

            return Results.Redirect(result.Value.AuthorizationUrl);
        })
        .WithTags(Tags.Authentication)
        .WithSummary("Starts a social login (302 to the provider).");

        app.MapGet("authentication/external/{provider}/callback", async (
            string provider,
            string? code,
            string? state,
            string? error,
            HttpContext httpContext,
            ISender sender,
            IOptions<AppOptions> appOptions,
            CancellationToken cancellationToken) =>
        {
            string? correlation = httpContext.Request.Cookies[CorrelationCookieName];

            httpContext.Response.Cookies.Delete(CorrelationCookieName, new CookieOptions { Path = CookiePath(httpContext) });

            Result<ExternalLoginResult> result = await sender.Send(
                new CompleteExternalLoginCommand(provider, code, state, error, correlation),
                cancellationToken);

            if (result.IsFailure)
            {
                return Results.Redirect(ErrorUrl(appOptions.Value, result.Error));
            }

            ExternalLoginResult login = result.Value;

            string fragment = login.Token is not null
                ? $"token={Uri.EscapeDataString(login.Token)}"
                : $"twoFactorToken={Uri.EscapeDataString(login.TwoFactorToken!)}";

            return Results.Redirect(
                $"{CallbackUrl(appOptions.Value)}#{fragment}&returnUrl={Uri.EscapeDataString(login.ReturnUrl)}");
        })
        .WithTags(Tags.Authentication)
        .WithSummary("The OAuth callback (302 to the frontend's /auth/callback).");
    }

    private static string CallbackUrl(AppOptions appOptions) => appOptions.FrontendUrl("auth/callback");

    private static string ErrorUrl(AppOptions appOptions, Error error) =>
        $"{CallbackUrl(appOptions)}#error={Uri.EscapeDataString(error.Code)}";

    // e.g. /api/v1/authentication/external: the cookie is only sent to the login endpoints.
    private static string CookiePath(HttpContext httpContext)
    {
        string path = httpContext.Request.PathBase + httpContext.Request.Path;
        int index = path.IndexOf("/authentication/external", StringComparison.OrdinalIgnoreCase);

        return index < 0 ? "/" : path[..(index + "/authentication/external".Length)];
    }
}
