using FluentValidation;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;

namespace Lodge.Application.Authentication.ResetPassword;

internal sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty()
            .WithError(ValidationErrors.ResetPassword.TokenIsRequired);

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithError(ValidationErrors.ResetPassword.PasswordIsRequired);
    }
}
