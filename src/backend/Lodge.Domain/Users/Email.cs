using Lodge.Domain.Core.Primitives;
using System.Text.RegularExpressions;

namespace Lodge.Domain.Users;

/// <summary>
/// Represents the email value object.
/// </summary>
public sealed record Email
{
    /// <summary>
    /// The email maximum length
    /// </summary>
    public const int MaxLength = 256;

    private const string EmailRegexPattern = @"^(?!\.)(""([^""\r\\]|\\[""\r\\])*""|([-a-z0-9!#$%&'*+/=?^_`{|}~]|(?<!\.)\.)*)(?<!\.)@[a-z0-9][\w\.-]*[a-z0-9]\.[a-z][a-z\.]*[a-z]$";

    private static readonly Lazy<Regex> EmailFormatRegex =
            new(() => new Regex(EmailRegexPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase));

    /// <summary>
    /// Initializes a new instance of the <see cref="Email"/> class.
    /// </summary>
    /// <param name="value">The email value.</param>
    private Email(string value) => Value = value;

    /// <summary>
    /// Gets the email value.
    /// </summary>
    public string Value { get; }

    public static implicit operator string(Email email) => email.Value;

    /// <summary>
    /// Creates a new <see cref="Email"/> instance based on the specified value.
    /// </summary>
    /// <param name="value">The email value.</param>
    /// <returns>The result of the email creation process containing the email or an error.</returns>
    public static Result<Email> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<Email>(EmailErrors.Empty);
        }

        if (value.Length > MaxLength)
        {
            return Result.Failure<Email>(EmailErrors.TooLong);
        }

        if (!EmailFormatRegex.Value.IsMatch(value))
        {
            return Result.Failure<Email>(EmailErrors.InvalidFormat);
        }

        return new Email(value);
    }
}
