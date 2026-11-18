using Lodge.Domain.Core.Primitives;

namespace Lodge.Domain.Users;

/// <summary>
/// Contains the two-factor authentication errors.
/// </summary>
public static class TwoFactorErrors
{
    public static readonly Error AlreadyEnabled = Error.Conflict(
        "TwoFactor.AlreadyEnabled",
        "Two-factor authentication is already enabled.");

    public static readonly Error NotSetUp = Error.Problem(
        "TwoFactor.NotSetUp",
        "Start the two-factor setup first.");

    public static readonly Error NotEnabled = Error.Problem(
        "TwoFactor.NotEnabled",
        "Two-factor authentication is not enabled.");

    public static readonly Error InvalidCode = Error.Problem(
        "TwoFactor.InvalidCode",
        "The verification code is incorrect.");

    public static readonly Error InvalidPassword = Error.Problem(
        "TwoFactor.InvalidPassword",
        "The password is incorrect.");
}
