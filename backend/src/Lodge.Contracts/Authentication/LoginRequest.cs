namespace Lodge.Contracts.Authentication;

/// <summary>
/// Represents the login request.
/// </summary>
/// <param name="Email">The email.</param>
/// <param name="Password">The password.</param>
public sealed record LoginRequest(string Email, string Password);
