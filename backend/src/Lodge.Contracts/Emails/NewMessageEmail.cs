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
