using Lodge.Application.Abstractions.Cryptography;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Users;

namespace Lodge.Application.Authentication.ResetPassword;

internal sealed class ResetPasswordCommandHandler(
    IAccountTokenService accountTokenService,
    IUserTokenRepository userTokenRepository,
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<ResetPasswordCommand>
{
    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        UserToken? token = await accountTokenService.FindUsableAsync(
            request.Token,
            UserTokenPurpose.PasswordReset,
            cancellationToken);

        if (token is null)
        {
            return Result.Failure(AuthenticationErrors.InvalidToken);
        }

        Result<Password> passwordResult = Password.Create(request.Password);

        if (passwordResult.IsFailure)
        {
            return Result.Failure(passwordResult.Error);
        }

        User? user = await userRepository.GetByIdAsync(token.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure(AuthenticationErrors.InvalidToken);
        }

        DateTime utcNow = dateTimeProvider.UtcNow;

        // Raises UserPasswordChangedDomainEvent, which sends the "password changed" e-mail.
        user.ChangePassword(passwordHasher.HashPassword(passwordResult.Value));

        // The link was delivered to the mailbox, which proves ownership of the address.
        user.VerifyEmail(utcNow);

        token.Consume(utcNow);

        foreach (UserToken outstanding in await userTokenRepository.GetUsableAsync(
                     user.Id, UserTokenPurpose.PasswordReset, utcNow, cancellationToken))
        {
            outstanding.Consume(utcNow);
        }

        return Result.Success();
    }
}
