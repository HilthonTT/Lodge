using FluentValidation;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;

namespace Lodge.Application.Notifications.GetUnreadCounts;

internal sealed class GetUnreadCountsQueryValidator : AbstractValidator<GetUnreadCountsQuery>
{
    public GetUnreadCountsQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithError(ValidationErrors.Notifications.UserIdIsRequired);
    }
}
