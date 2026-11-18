using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Application.Bookings.Confirm;
using Lodge.Application.Core.Errors;
using Lodge.Domain.Bookings;
using Lodge.Domain.Core.Guards;
using Lodge.Domain.Core.Primitives;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Stripe;
using CheckoutSession = Stripe.Checkout.Session;

namespace Lodge.Application.Stripe.Webhook;

/// <summary>
/// Represents the <see cref="StripeWebhookCommand"/> handler.
/// </summary>
/// <param name="httpContextAccessor">The http context accessor.</param>
/// <param name="bookingRepository">The booking repository.</param>
/// <param name="dateTimeProvider">The date time provider.</param>
/// <param name="configuration">The configuration.</param>
/// <param name="unitOfWork">The unit of work.</param>
/// <param name="publisher">The publisher.</param>
internal sealed class StripeWebhookCommandHandler(
    IHttpContextAccessor httpContextAccessor,
    IBookingRepository bookingRepository,
    IDateTimeProvider dateTimeProvider,
    IConfiguration configuration,
    IUnitOfWork unitOfWork,
    IPublisher publisher) : ICommandHandler<StripeWebhookCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(StripeWebhookCommand request, CancellationToken cancellationToken)
    {
        if (httpContextAccessor.HttpContext is null)
        {
            return Result.Failure(StripeErrors.HttpContextMissing);
        }

        string json = await new StreamReader(httpContextAccessor.HttpContext.Request.Body)
            .ReadToEndAsync(cancellationToken);

        string? stripeSignature = httpContextAccessor
            .HttpContext
            .Request
            .Headers["Stripe-Signature"];

        if (string.IsNullOrWhiteSpace(stripeSignature))
        {
            return Result.Failure(StripeErrors.SignatureMissing);
        }

        string? webhookSecret = configuration["Stripe:WebhookSecret"];
        Ensure.NotNullOrEmpty(webhookSecret, nameof(webhookSecret));

        Event stripeEvent = EventUtility.ConstructEvent(json, stripeSignature, webhookSecret);
        if (stripeEvent.Data.Object is not CheckoutSession checkoutSession)
        {
            return Result.Failure(StripeErrors.NoSessionFound);
        }

        Dictionary<string, string> metadata = checkoutSession.Metadata;

        if (!metadata.TryGetValue("userId", out string? userIdString) ||
            !metadata.TryGetValue("bookingId", out string? bookingIdString) ||
            !Guid.TryParse(userIdString, out Guid userId) ||
            !Guid.TryParse(bookingIdString, out Guid bookingId))
        {
            return Result.Failure(StripeErrors.InvalidMetadata);
        }

        switch (stripeEvent.Type)
        {
            case Events.CheckoutSessionCompleted:
                Result result = await ConfirmBookingAsync(userId, bookingId, cancellationToken);

                if (result.IsFailure)
                {
                    return Result.Failure(result.Error);
                }

                break;
        }

        return Result.Success();
    }

    /// <summary>
    /// Confirms the booking.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="bookingId">The booking identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An instance of <see cref="Result"/>.</returns>
    private async Task<Result> ConfirmBookingAsync(Guid userId, Guid bookingId, CancellationToken cancellationToken)
    {
        Booking? booking = await bookingRepository.GetByIdAsync(bookingId, cancellationToken);
        if (booking is null)
        {
            return Result.Failure(BookingErrors.NotFound(bookingId));
        }

        Result result = booking.Confirm(dateTimeProvider.UtcNow);
        if (result.IsFailure)
        {
            return Result.Failure(result.Error);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await publisher.Publish(
            new BookingConfirmedEvent(booking.Id, userId),
            cancellationToken);

        return Result.Success();
    }
}
