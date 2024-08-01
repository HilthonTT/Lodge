namespace Lodge.Contracts.Emails;

/// <summary>
/// Represents the booking reserved email.
/// </summary>
/// <param name="EmailTo">The email receiver.</param>
/// <param name="Name">The name receiver.</param>
public sealed record BookingReservedEmail(string EmailTo, string Name);
