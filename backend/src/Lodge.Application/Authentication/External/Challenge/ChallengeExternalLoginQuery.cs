using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Messaging;

namespace Lodge.Application.Authentication.External.Challenge;

/// <summary>
/// Starts a social login: returns the provider redirect and the correlation cookie value.
/// </summary>
/// <param name="Provider">The provider name (<c>google</c> or <c>github</c>).</param>
/// <param name="ReturnUrl">The relative path to return to after the login. Defaults to <c>/</c>.</param>
public sealed record ChallengeExternalLoginQuery(string Provider, string? ReturnUrl) : IQuery<ExternalLoginChallenge>;
