using FluentValidation;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;

namespace Lodge.Application.Users.TwoFactor.Setup;

internal sealed class SetupTwoFactorCommandValidator : AbstractValidator<SetupTwoFactorCommand>
{
    public SetupTwoFactorCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithError(ValidationErrors.TwoFactor.UserIdIsRequired);
    }
}
