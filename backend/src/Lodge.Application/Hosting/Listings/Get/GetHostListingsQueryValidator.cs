using FluentValidation;
using Lodge.Application.Core.Extensions;

namespace Lodge.Application.Hosting.Listings.Get;

internal sealed class GetHostListingsQueryValidator : AbstractValidator<GetHostListingsQuery>
{
    public GetHostListingsQueryValidator()
    {
        RuleFor(x => x.Page).ValidPage();
        RuleFor(x => x.PageSize).ValidPageSize();
    }
}
