using Lodge.Domain.Core.Guards;
using Lodge.Domain.Core.Primitives;

namespace Lodge.Domain.Users;

/// <summary>
/// Links a user to an account at an external identity provider (e.g. Google, GitHub).
/// </summary>
public sealed class ExternalLogin : Entity
{
    public const int ProviderMaxLength = 32;
    public const int ProviderKeyMaxLength = 256;

    private ExternalLogin(Guid id, Guid userId, string provider, string providerKey, DateTime createdOnUtc)
        : base(id)
    {
        UserId = userId;
        Provider = provider;
        ProviderKey = providerKey;
        CreatedOnUtc = createdOnUtc;
    }

    private ExternalLogin()
    {
    }

    public Guid UserId { get; private set; }

    /// <summary>
    /// Gets the lower-case provider name, e.g. <c>google</c>.
    /// </summary>
    public string Provider { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the user's stable identifier at the provider (the OpenID <c>sub</c> or the GitHub user id).
    /// </summary>
    public string ProviderKey { get; private set; } = string.Empty;

    public DateTime CreatedOnUtc { get; private set; }

    public static ExternalLogin Create(Guid userId, string provider, string providerKey, DateTime utcNow)
    {
        Ensure.NotNullOrEmpty(provider);
        Ensure.NotNullOrEmpty(providerKey);

        return new ExternalLogin(Guid.NewGuid(), userId, provider.ToLowerInvariant(), providerKey, utcNow);
    }
}
