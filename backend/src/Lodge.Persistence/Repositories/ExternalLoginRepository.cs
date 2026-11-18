using Lodge.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Lodge.Persistence.Repositories;

/// <summary>
/// Represents the <see cref="ExternalLogin"/> repository.
/// </summary>
internal sealed class ExternalLoginRepository(LodgeDbContext context) : IExternalLoginRepository
{
    public Task<ExternalLogin?> GetAsync(string provider, string providerKey, CancellationToken cancellationToken = default)
    {
        string normalizedProvider = provider.ToLowerInvariant();

        return context.ExternalLogins.FirstOrDefaultAsync(
            login => login.Provider == normalizedProvider && login.ProviderKey == providerKey,
            cancellationToken);
    }

    public void Insert(ExternalLogin externalLogin) => context.ExternalLogins.Add(externalLogin);
}
