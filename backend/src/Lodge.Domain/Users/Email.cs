using System.Text.RegularExpressions;
using Lodge.Domain.Core.Primitives;

namespace Lodge.Domain.Users;

/// <summary>
/// Represents an e-mail address. Values are trimmed and normalized to lower case so that
/// uniqueness checks and logins are case-insensitive.
/// </summary>
public sealed partial record Email
{
    public const int MaxLength = 256;

    private Email(string value) => Value = value;

    public string Value { get; }

    public static implicit operator string(Email email) => email.Value;

    public static Result<Email> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<Email>(EmailErrors.Empty);
        }

        string normalized = value.Trim().ToLowerInvariant();

        if (normalized.Length > MaxLength)
        {
            return Result.Failure<Email>(EmailErrors.TooLong);
        }

        if (!EmailFormatRegex().IsMatch(normalized))
        {
            return Result.Failure<Email>(EmailErrors.InvalidFormat);
        }

        return new Email(normalized);
    }

    [GeneratedRegex(
        @"^(?!\.)(""([^""\r\\]|\\[""\r\\])*""|([-a-z0-9!#$%&'*+/=?^_`{|}~]|(?<!\.)\.)*)(?<!\.)@[a-z0-9][\w\.-]*[a-z0-9]\.[a-z][a-z\.]*[a-z]$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex EmailFormatRegex();
}
