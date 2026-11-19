namespace Lodge.Contracts.Users;

/// <summary>
/// Represents the change password request.
/// </summary>
/// <param name="CurrentPassword">The current password, proving the caller is the account owner.</param>
/// <param name="Password">The new password.</param>
public sealed record ChangePasswordRequest(string CurrentPassword, string Password);
