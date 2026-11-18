namespace Lodge.Contracts.Emails;

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
