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
}
