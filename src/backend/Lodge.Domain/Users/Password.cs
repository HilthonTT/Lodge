using Lodge.Domain.Core.Primitives;

namespace Lodge.Domain.Users;

/// <summary>
/// Represents the password value object.
/// </summary>
public sealed record Password
{
    private const int MinPasswordLength = 6;

    private static readonly Func<char, bool> IsLower = c => c >= 'a' && c <= 'z';
    private static readonly Func<char, bool> IsUpper = c => c >= 'A' && c <= 'Z';
    private static readonly Func<char, bool> IsDigit = c => c >= '0' && c <= '9';
    private static readonly Func<char, bool> IsNonAlphaNumeric = c => !(IsLower(c) || IsUpper(c) || IsDigit(c));

    /// <summary>
    /// Initializes a new instance of the <see cref="Password"/> record.
    /// </summary>
    /// <param name="value">The password value.</param>
    private Password(string value) => Value = value;

    /// <summary>
    /// Gets the password value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Creates a new <see cref="Password"/> instance based on the specified value.
    /// </summary>
    /// <param name="value">The password value.</param>
    /// <returns>The result of the password creation containing the password or an error.</returns>
    public static Result<Password> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<Password>(PasswordErrors.Empty);
        }

        if (value.Length < MinPasswordLength)
        {
            return Result.Failure<Password>(PasswordErrors.TooShort);
        }

        if (!value.Any(IsLower))
        {
            return Result.Failure<Password>(PasswordErrors.MissingLowercaseLetter);
        }

        if (!value.Any(IsUpper))
        {
            return Result.Failure<Password>(PasswordErrors.MissingUppercaseLetter);
        }

        if (!value.Any(IsDigit))
        {
            return Result.Failure<Password>(PasswordErrors.MissingDigit);
        }

        if (!value.Any(IsNonAlphaNumeric))
        {
            return Result.Failure<Password>(PasswordErrors.MissingNonAlphaNumeric);
        }

        return new Password(value);
    }
}
