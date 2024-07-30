using Lodge.Domain.Core.Primitives;

namespace Lodge.Domain.Apartements;

/// <summary>
/// Represents the name's domain errors.
/// </summary>
public static class NameErrors
{
    public static readonly Error Empty = Error.Problem(
        "Name.Empty",
        "The name cannot be empty");

    public static readonly Error TooLong = Error.Problem(
        "Name.TooLong",
        "The name exceeds the 256 characters limit");
}
