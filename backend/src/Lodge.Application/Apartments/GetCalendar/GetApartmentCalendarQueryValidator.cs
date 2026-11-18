using FluentValidation;
using Lodge.Application.Core.Extensions;
using Lodge.Domain.Apartments;

namespace Lodge.Application.Apartments.GetCalendar;

internal sealed class GetApartmentCalendarQueryValidator : AbstractValidator<GetApartmentCalendarQuery>
{
    public GetApartmentCalendarQueryValidator()
    {
        RuleFor(x => x)
            .Must(x => CalendarRange.IsValid(x.From, x.To))
            .WithError(ApartmentErrors.InvalidCalendarRange);
    }
}
