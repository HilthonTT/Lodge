using FluentValidation;
using Lodge.Application.Core.Extensions;

namespace Lodge.Application.Hosting.Reviews;

internal sealed class GetHostReviewsQueryValidator : AbstractValidator<GetHostReviewsQuery>
{
    public GetHostReviewsQueryValidator()
    {
        RuleFor(x => x.Page).ValidPage();
        RuleFor(x => x.PageSize).ValidPageSize();
    }
}
