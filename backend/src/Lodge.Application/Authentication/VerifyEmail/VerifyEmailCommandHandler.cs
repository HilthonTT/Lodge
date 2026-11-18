using Lodge.Application.Abstractions.Messaging;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Users;

namespace Lodge.Application.Authentication.VerifyEmail;

internal sealed class VerifyEmailCommandHandler(
    IAccountTokenService accountTokenService,
    IUserRepository userRepository,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<VerifyEmailCommand>
{
    public async Task<Result> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        UserToken? token = await accountTokenService.FindUsableAsync(
            request.Token,
            UserTokenPurpose.EmailVerification,
            cancellationToken);

        if (token is null)
        {
            return Result.Failure(AuthenticationErrors.InvalidToken);
        }

        User? user = await userRepository.GetByIdAsync(token.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure(AuthenticationErrors.InvalidToken);
        }

        DateTime utcNow = dateTimeProvider.UtcNow;

        user.VerifyEmail(utcNow);
        token.Consume(utcNow);

        return Result.Success();
    }
}
