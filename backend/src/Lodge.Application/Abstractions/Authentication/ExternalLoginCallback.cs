namespace Lodge.Application.Abstractions.Authentication;

/// <summary>
/// The verified result of a provider callback.
/// </summary>
public sealed record ExternalLoginCallback(ExternalIdentity Identity, string ReturnUrl);
