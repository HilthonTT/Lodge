using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Cryptography;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Authentication;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Users;

namespace Lodge.Application.Users.Create;

/// <summary>
/// Represents the <see cref="CreateUserCommand"/> handler.
/// </summary>
/// <param name="userRepository">The user repository.</param>
/// <param name="passwordHasher">The password hasher.</param>
/// <param name="unitOfWork">The unit of work.</param>
/// <param name="jwtProvider">The JWT provider.</param>
internal sealed class CreateUserCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork,
    IJwtProvider jwtProvider) : ICommandHandler<CreateUserCommand, TokenResponse>
{
    /// <inheritdoc />
    public async Task<Result<TokenResponse>> Handle(
        CreateUserCommand request, 
        CancellationToken cancellationToken)
    {
        Result<FirstName> firstNameResult = FirstName.Create(request.FirstName);
        Result<LastName> lastNameResult = LastName.Create(request.LastName);
        Result<Email> emailResult = Email.Create(request.Email);
        Result<Password> passwordResult = Password.Create(request.Password);

        Result firstFailureOrSuccess = Result.FirstFailureOrSuccess(
            firstNameResult, lastNameResult, emailResult, passwordResult);

        if (firstFailureOrSuccess.IsFailure)
        {
            return Result.Failure<TokenResponse>(firstFailureOrSuccess.Error);
        }

        if (!await userRepository.IsEmailUniqueAsync(emailResult.Value, cancellationToken))
        {
            return Result.Failure<TokenResponse>(UserErrors.DuplicateEmail);
        }

        string passwordHash = passwordHasher.HashPassword(passwordResult.Value);

        var user = User.Create(firstNameResult.Value, lastNameResult.Value, emailResult.Value, passwordHash);

        userRepository.Insert(user);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        string token = jwtProvider.Create(user);

        var response = new TokenResponse(token);

        return response;
    }
}
