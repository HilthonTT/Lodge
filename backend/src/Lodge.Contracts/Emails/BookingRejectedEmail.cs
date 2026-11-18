namespace Lodge.Contracts.Emails;

/// <summary>
/// Represents the booking rejected email.
/// </summary>
/// <param name="EmailTo">The email receiver.</param>
/// <param name="Name">The name receiver.</param>
public sealed record BookingRejectedEmail(string EmailTo, string Name);
