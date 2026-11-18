namespace Lodge.Domain.Users;

/// <summary>
/// What a <see cref="UserToken"/> can be used for. Persisted by name.
/// </summary>
public enum UserTokenPurpose
{
    EmailVerification = 1,
    PasswordReset = 2,
}
