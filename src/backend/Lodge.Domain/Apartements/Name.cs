using Lodge.Domain.Core.Primitives;

namespace Lodge.Domain.Apartements;

/// <summary>
/// Represents the name value object.
/// </summary>
public sealed record Name
{
    /// <summary>
    /// The name maximum length.
    /// </summary>
    public const int MaxLength = 256;

    /// <summary>
    /// Initializes a new instance of the <see cref="Name"/> record.
    /// </summary>
    /// <param name="value">The name value.</param>
    private Name(string value) => Value = value;

    public string Value { get; }

    /// <summary>
    /// Creates a new <see cref="Name"/> instance based on the specified value.
    /// </summary>
    /// <param name="value">The name value.</param>
    /// <returns>
    /// The result of the name creation process containing the name or an error.
    /// </returns>
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
