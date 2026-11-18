using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Authentication;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Users;

namespace Lodge.Application.Authentication.Login;

/// <summary>
/// Represents the <see cref="LoginCommand"/> handler.
/// </summary>
/// <param name="userRepository">The user repository.</param>
/// <param name="passwordHashChecker">The password hash checker.</param>
/// <param name="jwtProvider">The JWT provider.</param>
internal sealed class LoginCommandHandler(
    IUserRepository userRepository,
    IPasswordHashChecker passwordHashChecker,
    IJwtProvider jwtProvider) : ICommandHandler<LoginCommand, TokenResponse>
{
    /// <inheritdoc />
    public async Task<Result<TokenResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        Result<Email> emailResult = Email.Create(request.Email);

        if (emailResult.IsFailure)
        {
            return Result.Failure<TokenResponse>(emailResult.Error);
        }

        User? user = await userRepository.GetByEmailAsync(emailResult.Value, cancellationToken);
        if (user is null)
        {
            return Result.Failure<TokenResponse>(AuthenticationErrors.InvalidEmailOrPassword);
        }

        bool passwordValid = user.VerifyPasswordHash(request.Password, passwordHashChecker);

        if (!passwordValid)
        {
            return Result.Failure<TokenResponse>(AuthenticationErrors.InvalidEmailOrPassword);
        }

        string token = jwtProvider.Create(user);

        var response = new TokenResponse(token);

        return response;
    }
}
