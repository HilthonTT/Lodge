namespace Lodge.Contracts.Users;

/// <summary>
/// Represents the change password request.
/// </summary>
/// <param name="Password">The password.</param>
public sealed record ChangePasswordRequest(string Password);
