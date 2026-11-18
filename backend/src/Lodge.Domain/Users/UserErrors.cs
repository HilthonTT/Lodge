using Lodge.Domain.Core.Primitives;

namespace Lodge.Domain.Users;

/// <summary>
/// Represents the user's domain errors.
/// </summary>
public static class UserErrors
{
    public static Error NotFound(Guid id) => Error.NotFound(
        "User.NotFound", 
        $"The user with the Id = '{id}' was not found.");

    public static Error InvalidPermissions => Error.Permission(
        "User.InvalidPermissions",
        "The current user does not have the permissions to perform that operation.");

    public static Error DuplicateEmail => Error.Conflict(
        "User.DuplicateEmail", 
        "The specified email is already in use.");

    public static Error CannotChangePassword => Error.Problem(
        "User.CannotChangePassword",
        "The password cannot be changed to the specified password.");

    public static Error EmailNotVerified => Error.Permission(
        "User.EmailNotVerified",
        "Verify your e-mail address before booking or hosting.");

    public static Error EmailAlreadyVerified => Error.Problem(
        "User.EmailAlreadyVerified",
        "The e-mail address is already verified.");

    public static Error VerificationEmailThrottled => Error.TooManyRequests(
        "User.VerificationEmailThrottled",
        "A verification e-mail was sent less than a minute ago. Please wait before requesting another one.");

    public static Error PasswordNotSet => Error.Problem(
        "User.PasswordNotSet",
        "This account has no password yet. Use 'Forgot password' to set one.");

    public static Error BioTooLong => Error.Validation(
        "User.BioTooLong",
        $"The bio must not be longer than {User.BioMaxLength} characters.");

    public static Error InvalidAvatar => Error.Validation(
        "User.InvalidAvatar",
        "The avatar must be a JPEG, PNG or WebP image.");

    public static Error AvatarTooLarge => Error.Validation(
        "User.AvatarTooLarge",
        "The avatar must not be larger than 5 MB.");
}
