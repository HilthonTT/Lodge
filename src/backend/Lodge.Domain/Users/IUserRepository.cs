namespace Lodge.Domain.Users;

/// <summary>
/// Represents the user repository interface.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Gets all the users.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of users.</returns>
    Task<List<User>> GetAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the user with the specified user.
    /// </summary>
    /// <param name="id">The user identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The instance that may contain the user with the specified identifier.</returns>
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the user with the specified email.
    /// </summary>
    /// <param name="email">The user email.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The instance that may contain the user with specified email.</returns>
    Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the specified email is unique.
    /// </summary>
    /// <param name="email">The email.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the specified email is unique, otherwise false.</returns>
    Task<bool> IsEmailUniqueAsync(Email email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts a user.
    /// </summary>
    /// <param name="user">The user to be inserted.</param>
    void Insert(User user);
}
