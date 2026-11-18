using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Authentication;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Users;

namespace Lodge.Application.Authentication.Login;

/// <summary>
/// Represents the <see cref="LoginCommand"/> handler. Users with two-factor authentication get a challenge token
/// instead of an access token.
/// </summary>
internal sealed class LoginCommandHandler(
    IUserRepository userRepository,
    IPasswordHashChecker passwordHashChecker,
    IJwtProvider jwtProvider) : ICommandHandler<LoginCommand, LoginResponse>
{
    /// <inheritdoc />
    public async Task<Result<LoginResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        Result<Email> emailResult = Email.Create(request.Email);

        if (emailResult.IsFailure)
        {
            return Result.Failure<LoginResponse>(emailResult.Error);
        }

        User? user = await userRepository.GetByEmailAsync(emailResult.Value, cancellationToken);

        // Accounts without a password (social login only) fail like a wrong password.
        if (user is null || !user.VerifyPasswordHash(request.Password, passwordHashChecker))
        {
            return Result.Failure<LoginResponse>(AuthenticationErrors.InvalidEmailOrPassword);
        }

        return user.TwoFactorEnabled
            ? LoginResponse.WithTwoFactorChallenge(jwtProvider.CreateTwoFactorChallenge(user))
            : LoginResponse.WithToken(jwtProvider.Create(user));
    }
}
