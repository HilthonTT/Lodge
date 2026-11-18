using FluentValidation;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;

namespace Lodge.Application.Authentication.ForgotPassword;

internal sealed class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().WithError(ValidationErrors.ForgotPassword.EmailIsRequired);
    }
}
