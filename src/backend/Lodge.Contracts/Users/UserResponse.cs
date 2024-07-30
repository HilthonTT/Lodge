namespace Lodge.Contracts.Users;

/// <summary>
/// Represents the user response.
/// </summary>
/// <param name="Id">The id.</param>
/// <param name="FirstName">The first name.</param>
/// <param name="LastName">The last name.</param>
/// <param name="Email">The email.</param>
/// <param name="CreatedOnUtc">The created on date and time in UTC format.</param>
public sealed record UserResponse(
    Guid Id, 
    string FirstName, 
    string LastName, 
    string Email, 
    Guid? ImageId,
    DateTime CreatedOnUtc);
