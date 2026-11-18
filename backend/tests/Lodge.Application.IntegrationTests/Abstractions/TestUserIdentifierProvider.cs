using Lodge.Application.Abstractions.Authentication;

namespace Lodge.Application.IntegrationTests.Abstractions;

/// <summary>
/// A settable stand-in for the HTTP-based user identifier provider.
/// </summary>
public sealed class TestUserIdentifierProvider : IUserIdentifierProvider
{
    public Guid UserId { get; set; }
}
