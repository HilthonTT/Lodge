using Lodge.Domain.Core.Primitives;

namespace Lodge.Domain.Users;

/// <summary>
/// Represents the last name's domain errors.
/// </summary>
public static class LastNameErrors
{
    public static readonly Error Empty = Error.Problem(
        "LastName.Empty", "Last name is empty");

    public static readonly Error TooLong = Error.Problem(
        "LastName.TooLong", "Last name exceeds the 128 characters limit");
}
