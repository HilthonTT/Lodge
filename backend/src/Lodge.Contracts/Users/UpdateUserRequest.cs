namespace Lodge.Contracts.Users;

/// <summary>
/// Represents the update user request.
/// </summary>
/// <param name="FirstName">The first name.</param>
/// <param name="LastName">The last name.</param>
/// <param name="ImageId">The image identifier.</param>
/// <param name="Bio">The biography (max. 500 characters). Null leaves it unchanged; an empty string clears it.</param>
public sealed record UpdateUserRequest(string FirstName, string LastName, Guid? ImageId, string? Bio = null);
