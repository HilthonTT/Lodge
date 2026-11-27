using FluentValidation;

namespace Lodge.Application.Notifications.UpdatePreferences;

internal sealed class UpdateNotificationPreferencesCommandValidator : AbstractValidator<UpdateNotificationPreferencesCommand>
{
    public UpdateNotificationPreferencesCommandValidator()
    {
        RuleFor(x => x.Preferences).NotNull();
        RuleFor(x => x.Preferences.Email).NotNull().When(x => x.Preferences is not null);
        RuleFor(x => x.Preferences.InApp).NotNull().When(x => x.Preferences is not null);
    }
}
