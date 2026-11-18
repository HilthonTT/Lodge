using Lodge.Domain.Users;

namespace Lodge.Application.Authentication;

/// <summary>
/// Issues the single-use tokens behind e-mailed links (e-mail verification, password reset) and sends the e-mails.
/// </summary>
public interface IAccountTokenService
{
    /// <summary>
    /// Returns true when a token with the specified purpose was issued to the user less than a minute ago.
    /// </summary>
    Task<bool> WasIssuedRecentlyAsync(Guid userId, UserTokenPurpose purpose, CancellationToken cancellationToken = default);

    /// <summary>
    /// Issues an e-mail verification token (valid 24 hours) and e-mails the link. E-mail failures are logged, not thrown.
    /// </summary>
    Task SendEmailVerificationAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Issues a password reset token (valid 1 hour) and e-mails the link. E-mail failures are logged, not thrown.
    /// </summary>
    Task SendPasswordResetAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds the token with the specified raw value and purpose when it is neither consumed nor expired.
    /// </summary>
    Task<UserToken?> FindUsableAsync(string? token, UserTokenPurpose purpose, CancellationToken cancellationToken = default);
}
