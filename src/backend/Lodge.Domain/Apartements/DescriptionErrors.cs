using Lodge.Domain.Core.Primitives;

namespace Lodge.Domain.Apartements;

/// <summary>
/// Represents the description's domain errors.
/// </summary>
public static class DescriptionErrors
{
    public static readonly Error Empty = Error.Problem(
        "Description.Empty", 
        "The description cannot be empty");

    public static readonly Error TooLong = Error.Problem(
        "Description.TooLong", 
        "The description exceeds the 1000 characters limit");
}
