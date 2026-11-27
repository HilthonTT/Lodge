using FluentValidation;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;

namespace Lodge.Application.Payments.CompleteSimulated;

internal sealed class CompleteSimulatedPaymentCommandValidator : AbstractValidator<CompleteSimulatedPaymentCommand>
{
    public CompleteSimulatedPaymentCommandValidator()
    {
        RuleFor(x => x.BookingId)
            .NotEmpty()
            .WithError(ValidationErrors.Payments.BookingIdIsRequired);
    }
}
