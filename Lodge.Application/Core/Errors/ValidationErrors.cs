using Lodge.Domain.Core.Primitives;

namespace Lodge.Application.Core.Errors;

/// <summary>
/// Contains the validation errors.
/// </summary>
internal static class ValidationErrors
{
    /// <summary>
    /// Contains the create user errors.
    /// </summary>
    internal static class CreateUser
    {
        internal static readonly Error FirstNameIsRequired = Error.Problem(
            "CreateUser.FirstNameIsRequired", "The first name is required");

        internal static readonly Error LastNameIsRequired = Error.Problem(
            "CreateUser.LastNameIsRequired", "The last name is required");

        internal static readonly Error EmailIsRequired = Error.Problem(
            "CreateUser.EmailIsRequired", "The email is required");

        internal static readonly Error PasswordIsRequired = Error.Problem(
            "CreateUser.PasswordIsRequired", "The password is required");
    }

    /// <summary>
    /// Contains the change password errors.
    /// </summary>
    internal static class ChangePassword
    {
        internal static readonly Error UserIdIsRequired = Error.Problem(
            "ChangePassword.UserIdIsRequired", "The user identifier is required");

        internal static readonly Error PasswordIsRequired = Error.Problem(
            "ChangePassword.PasswordIsRequired", "The password is required");
    }

    /// <summary>
    /// Contains the update user errors.
    /// </summary>
    internal static class UpdateUser
    {
        internal static readonly Error UserIdIsRequired = Error.Problem(
            "UpdateUser.UserIdIsRequired", "The user identifier is required");

        internal static readonly Error FirstNameIsRequired = Error.Problem(
            "UpdateUser.FirstNameIsRequired", "The first name is required");

        internal static readonly Error LastNameIsRequired = Error.Problem(
            "UpdateUser.LastNameIsRequired", "The last name is required");
    }
}
