namespace Lodge.Contracts.Emails;

/// <summary>
/// Represents the e-mail with the e-mail verification link.
/// </summary>
/// <param name="EmailTo">The email receiver.</param>
/// <param name="Name">The receiver's name.</param>
/// <param name="Link">The verification link.</param>
public sealed record EmailVerificationEmail(string EmailTo, string Name, string Link);
