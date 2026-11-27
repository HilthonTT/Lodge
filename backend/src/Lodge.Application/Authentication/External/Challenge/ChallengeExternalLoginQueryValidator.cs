using FluentValidation;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;

namespace Lodge.Application.Authentication.External.Challenge;

internal sealed class ChallengeExternalLoginQueryValidator : AbstractValidator<ChallengeExternalLoginQuery>
{
    public ChallengeExternalLoginQueryValidator()
    {
        RuleFor(x => x.Provider)
            .NotEmpty()
            .WithError(ValidationErrors.ExternalLogin.ProviderIsRequired);
    }
}
