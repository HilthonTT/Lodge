using Lodge.Application.Abstractions.Authentication;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Users;

namespace Lodge.Application.Hosting;

internal sealed class HostAccess(IUserIdentifierProvider userIdentifierProvider, IUserRepository userRepository)
    : IHostAccess
{
    public async Task<Result<HostCaller>> GetCallerAsync(CancellationToken cancellationToken)
    {
        Guid userId = userIdentifierProvider.UserId;

        User? user = await userRepository.GetByIdAsync(userId, cancellationToken);

        if (user is null)
        {
            return Result.Failure<HostCaller>(UserErrors.NotFound(userId));
        }

        if (!user.IsEmailVerified)
        {
            return Result.Failure<HostCaller>(UserErrors.EmailNotVerified);
        }

        return new HostCaller(user.Id, user.Role == UserRole.Admin);
    }
}
