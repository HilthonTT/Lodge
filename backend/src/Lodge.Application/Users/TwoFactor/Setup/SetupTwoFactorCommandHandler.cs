using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Cryptography;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Users;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Users;

namespace Lodge.Application.Users.TwoFactor.Setup;

internal sealed class SetupTwoFactorCommandHandler(
    IUserIdentifierProvider userIdentifierProvider,
    IUserRepository userRepository,
    ITotpProvider totpProvider,
    ISecretProtector secretProtector) : ICommandHandler<SetupTwoFactorCommand, TwoFactorSetupResponse>
{
    public async Task<Result<TwoFactorSetupResponse>> Handle(SetupTwoFactorCommand request, CancellationToken cancellationToken)
    {
        if (request.UserId != userIdentifierProvider.UserId)
        {
            return Result.Failure<TwoFactorSetupResponse>(UserErrors.InvalidPermissions);
        }

        User? user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure<TwoFactorSetupResponse>(UserErrors.NotFound(request.UserId));
        }

        string secret = totpProvider.GenerateSecret();

        Result result = user.BeginTwoFactorSetup(secretProtector.Protect(secret));

        if (result.IsFailure)
        {
            return Result.Failure<TwoFactorSetupResponse>(result.Error);
        }

        return new TwoFactorSetupResponse(secret, totpProvider.GetOtpAuthUri(secret, user.Email.Value));
    }
}
