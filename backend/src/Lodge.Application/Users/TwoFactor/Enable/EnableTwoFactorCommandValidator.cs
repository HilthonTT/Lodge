using FluentValidation;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;

namespace Lodge.Application.Users.TwoFactor.Enable;

internal sealed class EnableTwoFactorCommandValidator : AbstractValidator<EnableTwoFactorCommand>
{
    public EnableTwoFactorCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithError(ValidationErrors.TwoFactor.UserIdIsRequired);

        RuleFor(x => x.Code)
            .NotEmpty()
            .WithError(ValidationErrors.TwoFactor.CodeIsRequired);
    }
}
