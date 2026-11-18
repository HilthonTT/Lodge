namespace Lodge.Contracts.Emails;

/// <summary>
/// Reminds the guest of a trip two days before check-in.
/// </summary>
public sealed record TripReminderEmail(
    string EmailTo,
    string Name,
    string ListingName,
    DateOnly CheckIn,
    string? CheckInInstructions,
    string Link);
