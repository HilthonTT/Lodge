using FluentValidation;
using Lodge.Application.Core.Extensions;

namespace Lodge.Application.Reviews.GetByApartmentId;

internal sealed class GetReviewsByApartmentIdQueryValidator : AbstractValidator<GetReviewsByApartmentIdQuery>
{
    public GetReviewsByApartmentIdQueryValidator()
    {
        RuleFor(x => x.Page).ValidPage();
        RuleFor(x => x.PageSize).ValidPageSize();
    }
}
