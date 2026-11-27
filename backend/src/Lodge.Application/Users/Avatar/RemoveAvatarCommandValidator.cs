using FluentValidation;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;

namespace Lodge.Application.Users.Avatar;

internal sealed class RemoveAvatarCommandValidator : AbstractValidator<RemoveAvatarCommand>
{
    public RemoveAvatarCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithError(ValidationErrors.Users.UserIdIsRequired);
    }
}
