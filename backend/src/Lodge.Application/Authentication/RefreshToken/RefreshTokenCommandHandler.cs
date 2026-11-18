using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Authentication;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Users;

namespace Lodge.Application.Authentication.RefreshToken;

internal sealed class RefreshTokenCommandHandler(
    IUserIdentifierProvider userIdentifierProvider,
    IUserRepository userRepository,
    IJwtProvider jwtProvider) : ICommandHandler<RefreshTokenCommand, TokenResponse>
{
    public async Task<Result<TokenResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        Guid userId = userIdentifierProvider.UserId;

        User? user = await userRepository.GetByIdAsync(userId, cancellationToken);

        return user is null
            ? Result.Failure<TokenResponse>(UserErrors.NotFound(userId))
            : new TokenResponse(jwtProvider.Create(user));
    }
}
