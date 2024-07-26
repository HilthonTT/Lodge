namespace Lodge.Domain.Users;

/// <summary>
/// Represents the password hash checker interfaces.
/// </summary>
public interface IPasswordHashChecker
{
    /// <summary>
    /// Checks if the specified password hash and the provided passowrd hash match.
    /// </summary>
    /// <param name="passwordHash">The password hash.</param>
    /// <param name="providedPassword">The provided password hash.</param>
    /// <returns>True if the password hashes match, otherwise false.</returns>
    bool HashesMatch(string? passwordHash, string? providedPassword);
}
