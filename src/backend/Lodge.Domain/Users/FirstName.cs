using Lodge.Domain.Core.Primitives;

namespace Lodge.Domain.Users;

/// <summary>
/// Represents the first name value object.
/// </summary>
public sealed record FirstName
{
    /// <summary>
    /// The first name maximum length.
    /// </summary>
    public const int MaxLength = 128;

    /// <summary>
    /// Initializes a new instance of the <see cref="FirstName"/> record.
    /// </summary>
    /// <param name="value"></param>
    private FirstName(string value) => Value = value;

    /// <summary>
    /// Gets the first name value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Creates a new <see cref="FirstName"/> instance based on the specified value.
    /// </summary>
    /// <param name="value">The first name value.</param>
    /// <returns>
    /// The result of the first name creation process containing the first name or an error.
    /// </returns>
    public static Result<FirstName> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<FirstName>(FirstNameErrors.Empty);
        }

        if (value.Length > MaxLength)
        {
            return Result.Failure<FirstName>(FirstNameErrors.TooLong);
        }

        return new FirstName(value);
    }
}
