using Lodge.Domain.Core.Primitives;

namespace Lodge.Domain.Users;

/// <summary>
/// Represents the last name value object.
/// </summary>
public sealed record LastName
{
    /// <summary>
    /// The last name maximum length.
    /// </summary>
    public const int MaxLength = 128;

    /// <summary>
    /// Initializes a new instance of the <see cref="LastName"/> class.
    /// </summary>
    /// <param name="value">The last name value.</param>
    private LastName(string value) => Value = value;

    /// <summary>
    /// Gets the last name value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Creates a new <see cref="LastName"/> instance based on the specified value.
    /// </summary>
    /// <param name="value">The last name value.</param>
    /// <returns>The result of the last name creation process containing the last name or an error.</returns>
    public static Result<LastName> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<LastName>(LastNameErrors.Empty);
        }

        if (value.Length > MaxLength)
        {
            return Result.Failure<LastName>(LastNameErrors.TooLong);
        }

        return new LastName(value);
    }
}
