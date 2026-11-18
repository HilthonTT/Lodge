using Lodge.Application.Abstractions.App;
using Lodge.Application.Abstractions.Cryptography;
using Lodge.Application.Abstractions.Notifications;
using Lodge.Contracts.Emails;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Users;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lodge.Application.Authentication;

/// <summary>
/// The links are sent directly (not through the outbox), so the raw tokens never end up in the database.
/// </summary>
internal sealed partial class AccountTokenService(
    IUserTokenRepository userTokenRepository,
    IEmailNotificationService emailNotificationService,
    IDateTimeProvider dateTimeProvider,
    IOptions<AppOptions> appOptions,
    ILogger<AccountTokenService> logger) : IAccountTokenService
{
    public static readonly TimeSpan EmailVerificationLifetime = TimeSpan.FromHours(24);
    public static readonly TimeSpan PasswordResetLifetime = TimeSpan.FromHours(1);
    public static readonly TimeSpan ResendInterval = TimeSpan.FromSeconds(60);

    public async Task<bool> WasIssuedRecentlyAsync(
        Guid userId,
        UserTokenPurpose purpose,
        CancellationToken cancellationToken = default)
    {
        DateTime? lastIssuedOnUtc = await userTokenRepository.GetLastIssuedOnUtcAsync(userId, purpose, cancellationToken);

        return lastIssuedOnUtc is not null && dateTimeProvider.UtcNow - lastIssuedOnUtc.Value < ResendInterval;
    }

    public Task SendEmailVerificationAsync(User user, CancellationToken cancellationToken = default)
    {
        string token = Issue(user, UserTokenPurpose.EmailVerification, EmailVerificationLifetime);
        string link = appOptions.Value.FrontendUrl($"verify-email?token={Uri.EscapeDataString(token)}");

        return SendSafelyAsync(
            user,
            () => emailNotificationService.SendEmailVerificationEmailAsync(
                new EmailVerificationEmail(user.Email.Value, user.FullName, link),
                cancellationToken));
    }

    public Task SendPasswordResetAsync(User user, CancellationToken cancellationToken = default)
    {
        string token = Issue(user, UserTokenPurpose.PasswordReset, PasswordResetLifetime);
        string link = appOptions.Value.FrontendUrl($"reset-password?token={Uri.EscapeDataString(token)}");

        return SendSafelyAsync(
            user,
            () => emailNotificationService.SendPasswordResetEmailAsync(
                new PasswordResetEmail(user.Email.Value, user.FullName, link),
                cancellationToken));
    }

    public async Task<UserToken?> FindUsableAsync(
        string? token,
        UserTokenPurpose purpose,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        UserToken? userToken = await userTokenRepository.GetByHashAsync(
            SecureTokens.Hash(token.Trim()),
            purpose,
            cancellationToken);

        return userToken is not null && userToken.IsUsable(dateTimeProvider.UtcNow) ? userToken : null;
    }

    private string Issue(User user, UserTokenPurpose purpose, TimeSpan lifetime)
    {
        string token = SecureTokens.GenerateUrlSafeToken();

        userTokenRepository.Insert(UserToken.Create(
            user.Id,
            purpose,
            SecureTokens.Hash(token),
            dateTimeProvider.UtcNow,
            lifetime));

        return token;
    }

    private async Task SendSafelyAsync(User user, Func<Task> send)
    {
        try
        {
            await send();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // The user can request the e-mail again; a mail server outage must not fail the request.
            LogEmailFailed(logger, exception, user.Id);
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to send an account e-mail to user {UserId}")]
    private static partial void LogEmailFailed(ILogger logger, Exception exception, Guid userId);
}
