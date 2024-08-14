using FluentValidation;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;

namespace Lodge.Application.Stripe.Checkout;

/// <summary>
/// Represents the <see cref="StripeCheckoutCommand"/> validator.
/// </summary>
internal sealed class StripeCheckoutCommandValidator : AbstractValidator<StripeCheckoutCommand>
{
    /// <summary>
    /// Initializes a new instance of <see cref="StripeCheckoutCommandValidator"/> class.
    /// </summary>
    public StripeCheckoutCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithError(ValidationErrors.ConfirmBooking.UserIdIsRequired);

        RuleFor(x => x.BookingId)
            .NotEmpty()
            .WithError(ValidationErrors.ConfirmBooking.BookingIdIsRequired);
    }
}
