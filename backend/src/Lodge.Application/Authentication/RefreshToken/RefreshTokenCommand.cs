using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Authentication;

namespace Lodge.Application.Authentication.RefreshToken;

/// <summary>
/// Issues a new access token for the signed-in user, e.g. after the e-mail address was verified so that the
/// <c>emailVerified</c> claim is up to date.
/// </summary>
public sealed record RefreshTokenCommand : ICommand<TokenResponse>;
