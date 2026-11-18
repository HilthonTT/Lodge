using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Users;

namespace Lodge.Application.Authentication.ResendVerification;

internal sealed class ResendVerificationEmailCommandHandler(
    IUserIdentifierProvider userIdentifierProvider,
    IUserRepository userRepository,
    IAccountTokenService accountTokenService) : ICommandHandler<ResendVerificationEmailCommand>
{
    public async Task<Result> Handle(ResendVerificationEmailCommand request, CancellationToken cancellationToken)
    {
        Guid userId = userIdentifierProvider.UserId;

        User? user = await userRepository.GetByIdAsync(userId, cancellationToken);

        if (user is null)
        {
            return Result.Failure(UserErrors.NotFound(userId));
        }

        if (user.IsEmailVerified)
        {
            return Result.Failure(UserErrors.EmailAlreadyVerified);
        }

        if (await accountTokenService.WasIssuedRecentlyAsync(user.Id, UserTokenPurpose.EmailVerification, cancellationToken))
        {
            return Result.Failure(UserErrors.VerificationEmailThrottled);
        }

        await accountTokenService.SendEmailVerificationAsync(user, cancellationToken);

        return Result.Success();
    }
}
