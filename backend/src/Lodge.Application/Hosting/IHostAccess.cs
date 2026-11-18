using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Users;

namespace Lodge.Application.Hosting;

/// <summary>
/// Resolves the caller of a hosting endpoint. Any user with a verified e-mail address can host.
/// </summary>
internal interface IHostAccess
{
    /// <summary>
    /// Returns the caller, or <see cref="UserErrors.EmailNotVerified"/> (403) when the e-mail address is not verified.
    /// </summary>
    Task<Result<HostCaller>> GetCallerAsync(CancellationToken cancellationToken);
}
