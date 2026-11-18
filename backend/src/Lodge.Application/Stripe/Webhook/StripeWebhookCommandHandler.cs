using Lodge.Application.Abstractions.Messaging;
using Lodge.Application.Core.Errors;
using Lodge.Application.Experiences;
using Lodge.Application.Payments;
using Lodge.Application.Stripe.Checkout;
using Lodge.Domain.Bookings;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Experiences;
using Lodge.Domain.Payouts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using CheckoutSession = Stripe.Checkout.Session;

namespace Lodge.Application.Stripe.Webhook;

/// <summary>
/// Verifies the Stripe signature, then confirms the booking once its checkout session has been paid (recording the
/// payment intent and the amount, and scheduling the host payout), and keeps the hosts' payout accounts up to date
/// (<c>account.updated</c>). The handler is idempotent: Stripe retries deliveries, and replays succeed.
/// </summary>
internal sealed partial class StripeWebhookCommandHandler(
    IBookingRepository bookingRepository,
    IPayoutRepository payoutRepository,
    BookingPayments bookingPayments,
    IExperienceRepository experienceRepository,
    ExperiencePayments experiencePayments,
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

        // Platform events (checkout.session.*) and Connect events (account.updated) can come from two Stripe
        // endpoints with their own signing secrets: try the platform secret first, then the Connect one.
        Event? stripeEvent = null;
        string? lastError = null;

        foreach (string secret in options.WebhookSecrets)
        {
            try
            {
                stripeEvent = EventUtility.ConstructEvent(
                    request.Payload,
                    request.Signature,
                    secret,
                    throwOnApiVersionMismatch: false);
                break;
            }
            catch (StripeException exception)
            {
                lastError = exception.Message;
            }
        }

        if (stripeEvent is null)
        {
            LogInvalidSignature(logger, lastError ?? "No webhook secret matched");
            return Result.Failure(StripeErrors.InvalidSignature);
        }

        if (stripeEvent.Type == EventTypes.AccountUpdated && stripeEvent.Data.Object is Account account)
        {
            return await UpdatePayoutAccountAsync(account, cancellationToken);
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

        if (checkoutSession.Metadata?.TryGetValue(
                ExperienceCheckoutCommandHandler.ExperienceBookingIdMetadataKey,
                out string? experienceBookingIdValue) == true &&
            Guid.TryParse(experienceBookingIdValue, out Guid experienceBookingId))
        {
            ExperienceBooking? experienceBooking =
                await experienceRepository.GetBookingAsync(experienceBookingId, cancellationToken);

            if (experienceBooking is null)
            {
                return Result.Failure(ExperienceBookingErrors.NotFound(experienceBookingId));
            }

            decimal experienceAmountPaid = checkoutSession.AmountTotal is long experienceTotal
                ? experienceTotal / 100m
                : experienceBooking.TotalPrice;

            return await experiencePayments.ConfirmPaymentAsync(
                experienceBooking,
                checkoutSession.PaymentIntentId ?? checkoutSession.Id,
                experienceAmountPaid,
                cancellationToken);
        }

        if (checkoutSession.Metadata is null ||
            !checkoutSession.Metadata.TryGetValue(StripeCheckoutCommandHandler.BookingIdMetadataKey, out string? bookingIdValue) ||
            !Guid.TryParse(bookingIdValue, out Guid bookingId))
        {
            return Result.Failure(StripeErrors.InvalidMetadata);
        }

        Booking? booking = await bookingRepository.GetByIdAsync(bookingId, cancellationToken);

        if (booking is null)
        {
            return Result.Failure(BookingErrors.NotFound(bookingId));
        }

        decimal amountPaid = checkoutSession.AmountTotal is long total ? total / 100m : booking.TotalPrice.Amount;

        return await bookingPayments.ConfirmPaymentAsync(
            booking,
            checkoutSession.PaymentIntentId ?? checkoutSession.Id,
            amountPaid,
            cancellationToken);
    }

    private async Task<Result> UpdatePayoutAccountAsync(Account account, CancellationToken cancellationToken)
    {
        PayoutAccount? payoutAccount = await payoutRepository.GetAccountByExternalIdAsync(account.Id, cancellationToken);

        if (payoutAccount is null)
        {
            LogUnknownAccount(logger, account.Id);
            return Result.Success();
        }

        payoutAccount.Update(account.PayoutsEnabled);

        return Result.Success();
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Rejected Stripe webhook call: {Reason}")]
    private static partial void LogInvalidSignature(ILogger logger, string reason);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Ignoring Stripe event {EventType}")]
    private static partial void LogIgnoredEvent(ILogger logger, string eventType);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Ignoring account.updated for unknown account {AccountId}")]
    private static partial void LogUnknownAccount(ILogger logger, string accountId);
}
