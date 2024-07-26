using Lodge.Domain.Core.Primitives;

namespace Lodge.Domain.Apartements;

/// <summary>
/// Represents the name value object.
/// </summary>
public sealed record Name
{
    public const int MaxLength = 256;

    private Name(string value) => Value = value;

    public string Value { get; }

    /// <summary>
    /// Creates a new name value object with the specified value.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The newly created name result.</returns>
    public static Result<Name> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<Name>(NameErrors.Empty);
        }

        if (value.Length > MaxLength)
        {
            return Result.Failure<Name>(NameErrors.TooLong);
        }

        return new Name(value);
    }
}
