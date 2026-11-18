namespace Lodge.Contracts.Emails;

/// <summary>
/// Tells a participant about a new message (throttled to one e-mail per conversation every 15 minutes).
/// </summary>
/// <param name="EmailTo">The recipient.</param>
/// <param name="Name">The recipient's name.</param>
/// <param name="SenderFirstName">The author's first name.</param>
/// <param name="ListingName">The listing the conversation is about.</param>
/// <param name="Preview">The beginning of the message.</param>
/// <param name="Link">The absolute frontend link to the conversation.</param>
public sealed record NewMessageEmail(
    string EmailTo,
    string Name,
    string SenderFirstName,
    string ListingName,
    string Preview,
    string Link);

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

/// <summary>
/// Reminds the guest (or the host) to review a stay the day after check-out.
/// </summary>
/// <param name="ForHost">True when the host is asked to review the guest.</param>
public sealed record ReviewReminderEmail(
    string EmailTo,
    string Name,
    string ListingName,
    bool ForHost,
    DateOnly ReviewDeadline,
    string Link);
