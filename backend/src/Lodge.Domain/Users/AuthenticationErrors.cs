using Lodge.Domain.Core.Primitives;

namespace Lodge.Domain.Users;

/// <summary>
/// Contains the authentication errors.
/// </summary>
public static class AuthenticationErrors
{
    public static readonly Error InvalidEmailOrPassword = Error.Problem(
        "Authentication.InvalidEmailOrPassword",
        "The specified email or password are incorrect");

    public static readonly Error InvalidToken = Error.Problem(
        "Authentication.InvalidToken",
        "The link is invalid or has expired.");

    public static readonly Error InvalidTwoFactorToken = Error.Problem(
        "Authentication.InvalidTwoFactorToken",
        "The two-factor sign-in has expired. Please sign in again.");

    public static readonly Error InvalidTwoFactorCode = Error.Problem(
        "Authentication.InvalidTwoFactorCode",
        "The verification code or recovery code is incorrect.");
}
