using FluentValidation;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;

namespace Lodge.Application.Notifications.GetPreferences;

internal sealed class GetNotificationPreferencesQueryValidator : AbstractValidator<GetNotificationPreferencesQuery>
{
    public GetNotificationPreferencesQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithError(ValidationErrors.Notifications.UserIdIsRequired);
    }
}
