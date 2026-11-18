namespace Lodge.Contracts.Users;

/// <summary>
/// The new avatar.
/// </summary>
/// <param name="AvatarUrl">The absolute URL of the avatar image.</param>
public sealed record AvatarResponse(string AvatarUrl);
