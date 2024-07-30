using Lodge.Domain.Core.Primitives;

namespace Lodge.Domain.Users;

/// <summary>
/// Represents the email's domain errors
/// </summary>
public static class EmailErrors
{
    public static readonly Error Empty = Error.Problem(
        "Email.Empty", "Email is empty");

    public static readonly Error InvalidFormat = Error.Problem(
        "Email.InvalidFormat", "Email format is invalid");

    public static readonly Error TooLong = Error.Problem(
        "Email.TooLong", "Email exceeds the 256 characters limit");
}
