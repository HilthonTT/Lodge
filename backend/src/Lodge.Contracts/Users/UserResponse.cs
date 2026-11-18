namespace Lodge.Contracts.Users;

/// <summary>
/// Represents the user response.
/// </summary>
/// <param name="Id">The id.</param>
/// <param name="Email">The email.</param>
/// <param name="FirstName">The first name.</param>
/// <param name="LastName">The last name.</param>
/// <param name="ImageId">The profile image identifier.</param>
/// <param name="CreatedOnUtc">The created on date and time in UTC format.</param>
/// <param name="Role">The role: <c>"Guest"</c> or <c>"Admin"</c>.</param>
public sealed record UserResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    Guid? ImageId,
    DateTime CreatedOnUtc,
    string Role);
