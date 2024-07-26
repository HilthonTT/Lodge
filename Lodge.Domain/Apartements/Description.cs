using Lodge.Domain.Core.Primitives;

namespace Lodge.Domain.Apartements;

/// <summary>
/// Represents the description value object.
/// </summary>
public sealed record Description
{
    public const int MaxLength = 1000;

    private Description(string value) => Value = value;

    public string Value { get; }

    /// <summary>
    /// Creates a new description value object with the specified value.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The newly created description result.</returns>
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
