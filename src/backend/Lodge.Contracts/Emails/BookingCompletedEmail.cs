namespace Lodge.Contracts.Emails;

/// <summary>
/// Represents the booking completed email.
/// </summary>
/// <param name="EmailTo">The email receiver.</param>
/// <param name="Name">The name receiver.</param>
public sealed record BookingCompletedEmail(string EmailTo, string Name);
