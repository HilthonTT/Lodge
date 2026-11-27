using FluentValidation;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;

namespace Lodge.Application.Users.TwoFactor.Disable;

internal sealed class DisableTwoFactorCommandValidator : AbstractValidator<DisableTwoFactorCommand>
{
    public DisableTwoFactorCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithError(ValidationErrors.TwoFactor.UserIdIsRequired);

        RuleFor(x => x.Code)
            .NotEmpty()
            .WithError(ValidationErrors.TwoFactor.CodeIsRequired);
    }
}
