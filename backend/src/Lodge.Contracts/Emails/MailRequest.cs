namespace Lodge.Contracts.Emails;

/// <summary>
/// Represents the mail request.
/// </summary>
/// <param name="EmailTo">The email receiver.</param>
/// <param name="Subject">The subject.</param>
/// <param name="Body">The body.</param>
public sealed record MailRequest(string EmailTo, string Subject, string Body);
