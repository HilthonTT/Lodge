using Lodge.Domain.Core.Primitives;

namespace Lodge.Domain.Apartements;

/// <summary>
/// Represents the name's domain errors.
/// </summary>
public static class NameErrors
{
    public static readonly Error Empty = Error.Problem(
        "Name.Empty",
        "The Name cannot be empty");

    public static readonly Error TooLong = Error.Problem(
        "Name.TooLong",
        "The Name exceeds the 256 characters limit");
}
