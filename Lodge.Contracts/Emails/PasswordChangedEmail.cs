namespace Lodge.Contracts.Emails;

/// <summary>
/// Represents the password changed email.
/// </summary>
/// <param name="EmailTo">The email receiver.</param>
/// <param name="Name">The name.</param>
public sealed record PasswordChangedEmail(string EmailTo, string Name);
