namespace Lodge.Application.Abstractions.Authentication;

/// <summary>
/// A validated two-factor challenge token.
/// </summary>
/// <param name="UserId">The user who passed the first factor.</param>
/// <param name="TokenId">The unique token identifier (<c>jti</c>).</param>
public sealed record TwoFactorChallenge(Guid UserId, string TokenId);
