namespace Lodge.Contracts.Emails;

/// <summary>
/// Represents the notification email.
/// </summary>
/// <param name="EmailTo">The email receiver.</param>
/// <param name="Subject">The email subject.</param>
/// <param name="Body">The email body.</param>
public sealed record NotifcationEmail(string EmailTo, string Subject, string Body);
