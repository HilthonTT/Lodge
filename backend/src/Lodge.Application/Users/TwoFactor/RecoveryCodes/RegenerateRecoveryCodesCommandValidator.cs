using FluentValidation;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;

namespace Lodge.Application.Users.TwoFactor.RecoveryCodes;

internal sealed class RegenerateRecoveryCodesCommandValidator : AbstractValidator<RegenerateRecoveryCodesCommand>
{
    public RegenerateRecoveryCodesCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithError(ValidationErrors.TwoFactor.UserIdIsRequired);

        RuleFor(x => x.Code)
            .NotEmpty()
            .WithError(ValidationErrors.TwoFactor.CodeIsRequired);
    }
}
