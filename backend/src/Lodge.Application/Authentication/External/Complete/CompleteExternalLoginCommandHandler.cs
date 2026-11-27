using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Application.Core.Errors;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Users;

namespace Lodge.Application.Authentication.External.Complete;

/// <summary>
/// Resolves the user of an external identity:
/// <list type="number">
/// <item>by an existing external login;</item>
/// <item>otherwise by a matching e-mail address (verified by the provider and by Lodge), and links the login;</item>
/// <item>otherwise creates a new user with a verified e-mail and no password.</item>
/// </list>
/// </summary>
internal sealed class CompleteExternalLoginCommandHandler(
    IExternalAuthenticationService externalAuthenticationService,
    IExternalLoginRepository externalLoginRepository,
    IUserRepository userRepository,
    IJwtProvider jwtProvider,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<CompleteExternalLoginCommand, ExternalLoginResult>
{
    private const string AccessDeniedError = "access_denied";

    public async Task<Result<ExternalLoginResult>> Handle(
        CompleteExternalLoginCommand request,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(request.ProviderError))
        {
            return Result.Failure<ExternalLoginResult>(
                string.Equals(request.ProviderError, AccessDeniedError, StringComparison.Ordinal)
                    ? ExternalAuthenticationErrors.AccessDenied
                    : ExternalAuthenticationErrors.ProviderError);
        }

        Result<ExternalLoginCallback> callbackResult = await externalAuthenticationService.CompleteAsync(
            request.Provider,
            request.Code,
            request.State,
            request.Correlation,
            cancellationToken);

        if (callbackResult.IsFailure)
        {
            return Result.Failure<ExternalLoginResult>(callbackResult.Error);
        }

        Result<User> userResult = await ResolveUserAsync(callbackResult.Value.Identity, cancellationToken);

        if (userResult.IsFailure)
        {
            return Result.Failure<ExternalLoginResult>(userResult.Error);
        }

        User user = userResult.Value;
        string returnUrl = callbackResult.Value.ReturnUrl;

        return user.TwoFactorEnabled
            ? new ExternalLoginResult(null, jwtProvider.CreateTwoFactorChallenge(user), returnUrl)
            : new ExternalLoginResult(jwtProvider.Create(user), null, returnUrl);
    }

    private async Task<Result<User>> ResolveUserAsync(ExternalIdentity identity, CancellationToken cancellationToken)
    {
        ExternalLogin? existingLogin = await externalLoginRepository.GetAsync(
            identity.Provider,
            identity.ProviderKey,
            cancellationToken);

        if (existingLogin is not null)
        {
            User? linkedUser = await userRepository.GetByIdAsync(existingLogin.UserId, cancellationToken);

            return linkedUser is null
                ? Result.Failure<User>(UserErrors.NotFound(existingLogin.UserId))
                : linkedUser;
        }

        if (!identity.EmailVerified || Email.Create(identity.Email) is not { IsSuccess: true } emailResult)
        {
            return Result.Failure<User>(ExternalAuthenticationErrors.EmailNotVerified);
        }

        DateTime utcNow = dateTimeProvider.UtcNow;

        User? user = await userRepository.GetByEmailAsync(emailResult.Value, cancellationToken);

        if (user is not null)
        {
            // Linking to an unverified account would let whoever registered it (possibly not the owner of the
            // mailbox) keep access through their password.
            if (!user.IsEmailVerified)
            {
                return Result.Failure<User>(ExternalAuthenticationErrors.EmailInUse);
            }
        }
        else
        {
            user = User.CreateExternal(
                CreateFirstName(identity, emailResult.Value),
                CreateLastName(identity),
                emailResult.Value,
                utcNow);

            userRepository.Insert(user);
        }

        externalLoginRepository.Insert(ExternalLogin.Create(user.Id, identity.Provider, identity.ProviderKey, utcNow));

        return user;
    }

    private static FirstName CreateFirstName(ExternalIdentity identity, Email email)
    {
        string fallback = email.Value.Split('@')[0];
        string value = string.IsNullOrWhiteSpace(identity.FirstName) ? fallback : identity.FirstName.Trim();

        return FirstName.Create(Truncate(value, FirstName.MaxLength)).Value;
    }

    private static LastName CreateLastName(ExternalIdentity identity)
    {
        string value = string.IsNullOrWhiteSpace(identity.LastName) ? "-" : identity.LastName.Trim();

        return LastName.Create(Truncate(value, LastName.MaxLength)).Value;
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}
