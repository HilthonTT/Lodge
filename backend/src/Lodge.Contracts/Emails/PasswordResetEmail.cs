namespace Lodge.Contracts.Emails;

/// <summary>
/// Represents the e-mail with the password reset link.
/// </summary>
/// <param name="EmailTo">The email receiver.</param>
/// <param name="Name">The receiver's name.</param>
/// <param name="Link">The password reset link.</param>
public sealed record PasswordResetEmail(string EmailTo, string Name, string Link);
