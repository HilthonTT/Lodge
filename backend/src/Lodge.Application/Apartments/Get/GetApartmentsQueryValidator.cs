using FluentValidation;
using Lodge.Application.Core.Extensions;

namespace Lodge.Application.Apartments.Get;

internal sealed class GetApartmentsQueryValidator : AbstractValidator<GetApartmentsQuery>
{
    public GetApartmentsQueryValidator()
    {
        RuleFor(x => x.Page).ValidPage();
        RuleFor(x => x.PageSize).ValidPageSize();
    }
}
