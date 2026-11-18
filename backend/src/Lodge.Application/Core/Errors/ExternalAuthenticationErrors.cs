using Lodge.Domain.Core.Primitives;

namespace Lodge.Application.Core.Errors;

/// <summary>
/// Errors of the external (social) login flow. On the callback they are returned to the frontend as
/// <c>#error=&lt;code&gt;</c>.
/// </summary>
public static class ExternalAuthenticationErrors
{
    public static readonly Error ProviderNotConfigured = Error.NotFound(
        "External.ProviderNotConfigured", "The login provider is not supported or not configured.");

    public static readonly Error InvalidReturnUrl = Error.Validation(
        "External.InvalidReturnUrl", "The return URL must be a relative path on this site.");

    public static readonly Error InvalidState = Error.Problem(
        "External.InvalidState", "The login request has expired or was tampered with. Please try again.");

    public static readonly Error AccessDenied = Error.Problem(
        "External.AccessDenied", "The login was cancelled or denied at the provider.");

    public static readonly Error ProviderError = Error.Problem(
        "External.ProviderError", "The login provider could not be reached or returned an error.");

    public static readonly Error EmailNotVerified = Error.Problem(
        "External.EmailNotVerified", "The provider did not share a verified e-mail address.");

    public static readonly Error EmailInUse = Error.Conflict(
        "External.EmailInUse",
        "An account with this e-mail address already exists but its e-mail is not verified. " +
        "Sign in with your password and verify your e-mail first.");
}
