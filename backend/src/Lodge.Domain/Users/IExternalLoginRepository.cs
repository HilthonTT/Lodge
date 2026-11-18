namespace Lodge.Domain.Users;

/// <summary>
/// Represents the <see cref="ExternalLogin"/> repository.
/// </summary>
public interface IExternalLoginRepository
{
    Task<ExternalLogin?> GetAsync(string provider, string providerKey, CancellationToken cancellationToken = default);

    void Insert(ExternalLogin externalLogin);
}
