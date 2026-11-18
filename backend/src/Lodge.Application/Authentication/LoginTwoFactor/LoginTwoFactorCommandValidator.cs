using FluentValidation;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;

namespace Lodge.Application.Authentication.LoginTwoFactor;

internal sealed class LoginTwoFactorCommandValidator : AbstractValidator<LoginTwoFactorCommand>
{
    public LoginTwoFactorCommandValidator()
    {
        RuleFor(x => x.TwoFactorToken).NotEmpty().WithError(ValidationErrors.LoginTwoFactor.TwoFactorTokenIsRequired);

        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.Code) || !string.IsNullOrWhiteSpace(x.RecoveryCode))
            .WithError(ValidationErrors.LoginTwoFactor.CodeIsRequired);
    }
}
