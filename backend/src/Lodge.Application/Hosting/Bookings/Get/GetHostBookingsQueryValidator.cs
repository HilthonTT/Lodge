using FluentValidation;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;
using Lodge.Domain.Bookings;

namespace Lodge.Application.Hosting.Bookings.Get;

internal sealed class GetHostBookingsQueryValidator : AbstractValidator<GetHostBookingsQuery>
{
    public GetHostBookingsQueryValidator()
    {
        RuleFor(x => x.Page).ValidPage();
        RuleFor(x => x.PageSize).ValidPageSize();

        RuleFor(x => x.Status)
            .Must(status => status is null || Enum.IsDefined((BookingStatus)status.Value))
            .WithError(ValidationErrors.GetBookings.InvalidStatus);
    }
}
