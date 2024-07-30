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
}
