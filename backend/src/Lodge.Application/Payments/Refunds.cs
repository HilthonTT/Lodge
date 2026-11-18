using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Domain.Bookings;
using Lodge.Domain.Bookings.Events;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Experiences;
using Microsoft.Extensions.Logging;
using Stripe;

namespace Lodge.Application.Payments;

/// <summary>
/// Published to the message broker when a cancelled, paid booking has a refund to send.
/// </summary>
public sealed record BookingRefundRequestedIntegrationEvent(Guid BookingId) : IIntegrationEvent;

internal sealed class PublishIntegrationEventOnBookingRefundRequestedDomainEventHandler(
    IIntegrationEventPublisher integrationEventPublisher) : IDomainEventHandler<BookingRefundRequestedDomainEvent>
{
    public Task Handle(BookingRefundRequestedDomainEvent notification, CancellationToken cancellationToken) =>
        integrationEventPublisher.PublishAsync(
            new BookingRefundRequestedIntegrationEvent(notification.BookingId),
            cancellationToken);
}

/// <summary>
/// Sends the pending refund of a cancelled booking (called by the integration event consumer).
/// </summary>
public interface IRefundProcessor
{
    /// <summary>
    /// Refunds the booking through Stripe (idempotency key = booking id) or simulates it, and records the outcome
    /// (<see cref="RefundStatus.Succeeded"/> or <see cref="RefundStatus.Failed"/>). Does nothing unless the refund
    /// is <see cref="RefundStatus.Pending"/>.
    /// </summary>
    Task ProcessAsync(Guid bookingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refunds a cancelled experience booking the same way (idempotency key = experience booking id).
    /// </summary>
    Task ProcessExperienceAsync(Guid experienceBookingId, CancellationToken cancellationToken = default);
}

internal sealed partial class RefundProcessor(
    IBookingRepository bookingRepository,
    IExperienceRepository experienceRepository,
    IUnitOfWork unitOfWork,
    PaymentGateways paymentGateways,
    IDateTimeProvider dateTimeProvider,
    ILogger<RefundProcessor> logger) : IRefundProcessor
{
    public async Task ProcessAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        Booking? booking = await bookingRepository.GetByIdAsync(bookingId, cancellationToken);

        if (booking is not { RefundStatus: RefundStatus.Pending, PaymentIntentId: not null, RefundAmount: > 0 })
        {
            return;
        }

        try
        {
            string refundId = await paymentGateways.For(booking.PaymentIntentId).RefundAsync(
                booking.PaymentIntentId,
                booking.RefundAmount.Value,
                booking.TotalPrice.Currency.Code,
                booking.Id.ToString(),
                cancellationToken);

            booking.MarkRefundSucceeded(refundId, dateTimeProvider.UtcNow);
        }
        catch (StripeException exception)
        {
            LogRefundFailed(logger, exception, booking.Id);
            booking.MarkRefundFailed();
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task ProcessExperienceAsync(Guid experienceBookingId, CancellationToken cancellationToken = default)
    {
        ExperienceBooking? booking = await experienceRepository.GetBookingAsync(experienceBookingId, cancellationToken);

        if (booking is not { RefundStatus: RefundStatus.Pending, PaymentIntentId: not null, RefundAmount: > 0 })
        {
            return;
        }

        try
        {
            string refundId = await paymentGateways.For(booking.PaymentIntentId).RefundAsync(
                booking.PaymentIntentId,
                booking.RefundAmount.Value,
                booking.Currency,
                $"experience-{booking.Id}",
                cancellationToken);

            booking.MarkRefundSucceeded(refundId, dateTimeProvider.UtcNow);
        }
        catch (StripeException exception)
        {
            LogRefundFailed(logger, exception, booking.Id);
            booking.MarkRefundFailed();
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "The refund of booking {BookingId} failed")]
    private static partial void LogRefundFailed(ILogger logger, Exception exception, Guid bookingId);
}
