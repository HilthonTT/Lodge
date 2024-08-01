namespace Lodge.Contracts.Emails;

/// <summary>
/// Represents the booking cancelled email.
/// </summary>
/// <param name="EmailTo">The email receiver.</param>
/// <param name="Name">The name receiver.</param>
public sealed record BookingCancelledEmail(string EmailTo, string Name);
