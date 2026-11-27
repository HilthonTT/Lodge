using FluentValidation;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;

namespace Lodge.Application.Stripe.Webhook;

internal sealed class StripeWebhookCommandValidator : AbstractValidator<StripeWebhookCommand>
{
    public StripeWebhookCommandValidator()
    {
        RuleFor(x => x.Payload)
            .NotEmpty()
            .WithError(ValidationErrors.StripeWebhook.PayloadIsRequired);
    }
}
