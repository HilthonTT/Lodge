using FluentValidation;
using Lodge.Application.Apartments.GetCalendar;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;
using Lodge.Application.Hosting.Listings;
using Lodge.Domain.Apartments;

namespace Lodge.Application.Hosting.Calendar;

internal sealed class UpdateCalendarCommandValidator : AbstractValidator<UpdateCalendarCommand>
{
    public UpdateCalendarCommandValidator()
    {
        RuleFor(x => x)
            .Must(x => CalendarRange.IsValid(x.From, x.To))
            .WithError(ApartmentErrors.InvalidCalendarRange);

        RuleFor(x => x)
            .Must(x => x.Blocked.HasValue || x.Price.HasValue || x.ClearPrice)
            .WithError(ValidationErrors.Calendar.NothingToChange);

        RuleFor(x => x)
            .Must(x => !(x.Price.HasValue && x.ClearPrice))
            .WithError(ValidationErrors.Calendar.PriceAndClearPrice);

        RuleFor(x => x.Price)
            .Must(price => price is null or (> 0 and <= ListingRequestValidator.MaxPrice))
            .WithError(ApartmentErrors.InvalidPrice);
    }
}
