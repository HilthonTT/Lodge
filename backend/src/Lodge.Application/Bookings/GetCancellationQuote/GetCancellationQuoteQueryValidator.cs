using FluentValidation;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;

namespace Lodge.Application.Bookings.GetCancellationQuote;

internal sealed class GetCancellationQuoteQueryValidator : AbstractValidator<GetCancellationQuoteQuery>
{
    public GetCancellationQuoteQueryValidator()
    {
        RuleFor(x => x.BookingId)
            .NotEmpty()
            .WithError(ValidationErrors.Bookings.BookingIdIsRequired);
    }
}
