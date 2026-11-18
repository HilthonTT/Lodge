using System.Security.Cryptography;
using Lodge.Application.Abstractions.Cryptography;
using Microsoft.AspNetCore.DataProtection;

namespace Lodge.Infrastructure.Cryptography;

/// <summary>
/// Encrypts secrets with ASP.NET Core Data Protection. The key ring is persisted in PostgreSQL
/// (see <c>Lodge.Persistence</c>), so encrypted values survive restarts and work across instances.
/// </summary>
internal sealed class DataProtectionSecretProtector(IDataProtectionProvider dataProtectionProvider) : ISecretProtector
{
    private const string Purpose = "Lodge.Secrets.v1";

    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector(Purpose);

    public string Protect(string plaintext) => _protector.Protect(plaintext);

    public string? Unprotect(string protectedValue)
    {
        try
        {
            return _protector.Unprotect(protectedValue);
        }
        catch (CryptographicException)
        {
            return null;
        }
    }
}
