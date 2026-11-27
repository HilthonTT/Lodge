using FluentValidation;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;

namespace Lodge.Application.Experiences.GetById;

internal sealed class GetExperienceByIdQueryValidator : AbstractValidator<GetExperienceByIdQuery>
{
    public GetExperienceByIdQueryValidator()
    {
        RuleFor(x => x.ExperienceId)
            .NotEmpty()
            .WithError(ValidationErrors.Experiences.ExperienceIdIsRequired);
    }
}
