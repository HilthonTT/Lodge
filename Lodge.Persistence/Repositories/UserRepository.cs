using Lodge.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Lodge.Persistence.Repositories;

/// <summary>
/// Represents the user repository.
/// </summary>
/// <param name="context">The database context.</param>
internal sealed class UserRepository(LodgeDbContext context) : IUserRepository
{
    /// <inheritdoc />
    public Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        return context.Users.FirstOrDefaultAsync(user => user.Email.Value == email.Value, cancellationToken);
    }

    /// <inheritdoc />
    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return context.Users.FirstOrDefaultAsync(user => user.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> IsEmailUniqueAsync(Email email, CancellationToken cancellationToken = default)
    {
        return !await context.Users.AnyAsync(user => user.Email.Value == email.Value, cancellationToken);
    }

    /// <inheritdoc />
    public void Insert(User user)
    {
        context.Users.Add(user);
    }
}
