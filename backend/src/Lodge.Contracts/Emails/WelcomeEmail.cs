namespace Lodge.Contracts.Emails;

/// <summary>
/// Represents the welcome email.
/// </summary>
/// <param name="EmailTo">The email receiver.</param>
/// <param name="Name">The name receiver.</param>
public sealed record WelcomeEmail(string EmailTo, string Name);
