using FluentValidation;
using Lodge.Application.Core.Extensions;

namespace Lodge.Application.Experiences;

internal sealed class GetHostExperienceBookingsQueryValidator : AbstractValidator<GetHostExperienceBookingsQuery>
{
    public GetHostExperienceBookingsQueryValidator()
    {
        RuleFor(x => x.Page).ValidPage();
        RuleFor(x => x.PageSize).ValidPageSize();
    }
}
