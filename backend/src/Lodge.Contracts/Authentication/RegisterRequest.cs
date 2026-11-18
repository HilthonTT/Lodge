namespace Lodge.Contracts.Authentication;

/// <summary>
/// Represents the register request.
/// </summary>
/// <param name="FirstName">The first name.</param>
/// <param name="LastName">The last name.</param>
/// <param name="Email">The email.</param>
/// <param name="Password">The password.</param>
public sealed record RegisterRequest(
    string FirstName, 
    string LastName, 
    string Email, 
    string Password);
