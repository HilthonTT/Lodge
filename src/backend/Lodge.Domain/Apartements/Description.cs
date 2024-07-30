using Lodge.Domain.Core.Primitives;

namespace Lodge.Domain.Apartements;

/// <summary>
/// Represents the description value object.
/// </summary>
public sealed record Description
{
    /// <summary>
    /// The description maximum length.
    /// </summary>
    public const int MaxLength = 1000;

    /// <summary>
    /// Initializes a new instance of the <see cref="Description"/> record.
    /// </summary>
    /// <param name="value">The description value.</param>
    private Description(string value) => Value = value;

    /// <summary>
    /// Gets the description value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Creates a new <see cref="Description"/> instance based on the specified value.
    /// </summary>
    /// <param name="value">The description value.</param>
    /// <returns>
    /// The result of the description creation process containing the description or an error.
    /// </returns>
    public static Result<Description> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<Description>(DescriptionErrors.Empty);
        }

        if (value.Length > MaxLength)
        {
            return Result.Failure<Description>(DescriptionErrors.TooLong);
        }

        return new Description(value);
    }
}
