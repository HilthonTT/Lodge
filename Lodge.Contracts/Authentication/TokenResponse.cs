namespace Lodge.Contracts.Authentication;

/// <summary>
/// Represents a token response.
/// </summary>
/// <param name="Token">The token value.</param>
public sealed record TokenResponse(string Token);
