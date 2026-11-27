using FluentValidation;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;

namespace Lodge.Application.Files.GetById;

internal sealed class GetFileByIdQueryValidator : AbstractValidator<GetFileByIdQuery>
{
    public GetFileByIdQueryValidator()
    {
        RuleFor(x => x.FileId)
            .NotEmpty()
            .WithError(ValidationErrors.Files.FileIdIsRequired);
    }
}
