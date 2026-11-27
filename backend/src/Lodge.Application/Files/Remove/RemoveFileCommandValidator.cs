using FluentValidation;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;

namespace Lodge.Application.Files.Remove;

internal sealed class RemoveFileCommandValidator : AbstractValidator<RemoveFileCommand>
{
    public RemoveFileCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithError(ValidationErrors.Files.UserIdIsRequired);

        RuleFor(x => x.FileId)
            .NotEmpty()
            .WithError(ValidationErrors.Files.FileIdIsRequired);
    }
}
