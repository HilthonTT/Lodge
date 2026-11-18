using FluentValidation;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;
using Lodge.Domain.Payouts;

namespace Lodge.Application.Payouts;

internal sealed class GetPayoutsQueryValidator : AbstractValidator<GetPayoutsQuery>
{
    public GetPayoutsQueryValidator()
    {
        RuleFor(x => x.Page).ValidPage();
        RuleFor(x => x.PageSize).ValidPageSize();

        RuleFor(x => x.Status)
            .Must(status => string.IsNullOrWhiteSpace(status) || EnumNames.TryParse<PayoutStatus>(status, out _))
            .WithError(ValidationErrors.Payouts.InvalidStatus);
    }
}
