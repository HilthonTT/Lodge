using Lodge.Application.Abstractions.Messaging;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Users;

namespace Lodge.Application.Authentication.ForgotPassword;

internal sealed class ForgotPasswordCommandHandler(
    IUserRepository userRepository,
    IAccountTokenService accountTokenService) : ICommandHandler<ForgotPasswordCommand>
{
    public async Task<Result> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        Result<Email> emailResult = Email.Create(request.Email);

        if (emailResult.IsFailure)
        {
            return Result.Success();
        }

        User? user = await userRepository.GetByEmailAsync(emailResult.Value, cancellationToken);

        // Unknown e-mails and repeated requests (at most one e-mail per minute) succeed silently.
        if (user is null ||
            await accountTokenService.WasIssuedRecentlyAsync(user.Id, UserTokenPurpose.PasswordReset, cancellationToken))
        {
            return Result.Success();
        }

        await accountTokenService.SendPasswordResetAsync(user, cancellationToken);

        return Result.Success();
    }
}
