namespace Lodge.Contracts.Users;

/// <summary>
/// Represents the user response (the signed-in user's own account).
/// </summary>
/// <param name="Id">The id.</param>
/// <param name="Email">The email.</param>
/// <param name="FirstName">The first name.</param>
/// <param name="LastName">The last name.</param>
/// <param name="ImageId">The profile image (avatar) file identifier.</param>
/// <param name="CreatedOnUtc">The created on date and time in UTC format.</param>
/// <param name="Role">The role: <c>"Guest"</c> or <c>"Admin"</c>.</param>
/// <param name="EmailVerified">Whether the e-mail address is verified.</param>
/// <param name="TwoFactorEnabled">Whether two-factor authentication is enabled.</param>
/// <param name="HasPassword">False for accounts created through a social login that never set a password.</param>
/// <param name="ExternalLogins">The linked social login providers (<c>google</c>, <c>github</c>).</param>
/// <param name="AvatarUrl">The absolute avatar URL, or null.</param>
/// <param name="Bio">The biography, or null.</param>
public sealed record UserResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    Guid? ImageId,
    DateTime CreatedOnUtc,
    string Role,
    bool EmailVerified,
    bool TwoFactorEnabled,
    bool HasPassword,
    IReadOnlyList<string> ExternalLogins,
    string? AvatarUrl,
    string? Bio);
