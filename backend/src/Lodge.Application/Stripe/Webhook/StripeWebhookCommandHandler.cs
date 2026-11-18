using Lodge.Application.Abstractions.Messaging;
using Lodge.Application.Core.Errors;
using Lodge.Application.Stripe.Checkout;
using Lodge.Domain.Bookings;
using Lodge.Domain.Core.Primitives;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using CheckoutSession = Stripe.Checkout.Session;

namespace Lodge.Application.Stripe.Webhook;

/// <summary>
/// Verifies the Stripe signature and confirms the booking once its checkout session has been paid.
/// The handler is idempotent: Stripe retries deliveries, and replays of an already processed event succeed.
/// </summary>
internal sealed partial class StripeWebhookCommandHandler(
    IBookingRepository bookingRepository,
    IDateTimeProvider dateTimeProvider,
    IOptions<StripeOptions> stripeOptions,
    ILogger<StripeWebhookCommandHandler> logger) : ICommandHandler<StripeWebhookCommand>
{
    private const string PaidStatus = "paid";

    public async Task<Result> Handle(StripeWebhookCommand request, CancellationToken cancellationToken)
    {
        StripeOptions options = stripeOptions.Value;

        if (!options.IsWebhookConfigured)
        {
            return Result.Failure(StripeErrors.NotConfigured);
        }

        if (string.IsNullOrWhiteSpace(request.Signature))
        {
            return Result.Failure(StripeErrors.SignatureMissing);
        }

        Event stripeEvent;

        try
        {
            stripeEvent = EventUtility.ConstructEvent(
                request.Payload,
                request.Signature,
                options.WebhookSecret,
                throwOnApiVersionMismatch: false);
        }
        catch (StripeException exception)
        {
            LogInvalidSignature(logger, exception.Message);
            return Result.Failure(StripeErrors.InvalidSignature);
        }

        bool isPaymentEvent = stripeEvent.Type is
            EventTypes.CheckoutSessionCompleted or
            EventTypes.CheckoutSessionAsyncPaymentSucceeded;

        if (!isPaymentEvent || stripeEvent.Data.Object is not CheckoutSession checkoutSession)
        {
            LogIgnoredEvent(logger, stripeEvent.Type);
            return Result.Success();
        }

        if (checkoutSession.PaymentStatus != PaidStatus)
        {
            // Delayed payment methods: wait for checkout.session.async_payment_succeeded.
            return Result.Success();
        }

        if (checkoutSession.Metadata is null ||
            !checkoutSession.Metadata.TryGetValue(StripeCheckoutCommandHandler.BookingIdMetadataKey, out string? bookingIdValue) ||
            !Guid.TryParse(bookingIdValue, out Guid bookingId))
        {
            return Result.Failure(StripeErrors.InvalidMetadata);
        }

        return await ConfirmBookingAsync(bookingId, cancellationToken);
    }

    private async Task<Result> ConfirmBookingAsync(Guid bookingId, CancellationToken cancellationToken)
    {
        Booking? booking = await bookingRepository.GetByIdAsync(bookingId, cancellationToken);

        if (booking is null)
        {
            return Result.Failure(BookingErrors.NotFound(bookingId));
        }

        if (booking.Status is BookingStatus.Confirmed or BookingStatus.Completed)
        {
            return Result.Success();
        }

        return booking.Confirm(dateTimeProvider.UtcNow);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Rejected Stripe webhook call: {Reason}")]
    private static partial void LogInvalidSignature(ILogger logger, string reason);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Ignoring Stripe event {EventType}")]
    private static partial void LogIgnoredEvent(ILogger logger, string eventType);
}
