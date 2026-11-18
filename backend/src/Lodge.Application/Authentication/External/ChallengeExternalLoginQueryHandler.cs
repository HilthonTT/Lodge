using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Application.Core.Errors;
using Lodge.Domain.Core.Primitives;

namespace Lodge.Application.Authentication.External;

internal sealed class ChallengeExternalLoginQueryHandler(IExternalAuthenticationService externalAuthenticationService)
    : IQueryHandler<ChallengeExternalLoginQuery, ExternalLoginChallenge>
{
    public Task<Result<ExternalLoginChallenge>> Handle(ChallengeExternalLoginQuery request, CancellationToken cancellationToken)
    {
        string returnUrl = string.IsNullOrEmpty(request.ReturnUrl) ? ReturnUrls.Default : request.ReturnUrl;

        if (!ReturnUrls.IsLocal(returnUrl))
        {
            return Task.FromResult(Result.Failure<ExternalLoginChallenge>(ExternalAuthenticationErrors.InvalidReturnUrl));
        }

        return Task.FromResult(externalAuthenticationService.CreateChallenge(request.Provider, returnUrl));
    }
}
