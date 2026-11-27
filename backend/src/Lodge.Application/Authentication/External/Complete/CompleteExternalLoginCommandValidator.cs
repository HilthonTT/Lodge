using FluentValidation;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;

namespace Lodge.Application.Authentication.External.Complete;

internal sealed class CompleteExternalLoginCommandValidator : AbstractValidator<CompleteExternalLoginCommand>
{
    public CompleteExternalLoginCommandValidator()
    {
        RuleFor(x => x.Provider)
            .NotEmpty()
            .WithError(ValidationErrors.ExternalLogin.ProviderIsRequired);
    }
}
