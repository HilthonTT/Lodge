using Lodge.Contracts.Emails;

namespace Lodge.Application.Abstractions.Emails;

/// <summary>
/// Represents the email service interface.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends the email with the content based on the specified mail request.
    /// </summary>
    /// <param name="mailRequest">The mail request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The completed task.</returns>
    Task SendEmailAsync(MailRequest mailRequest, CancellationToken cancellationToken = default);
}
