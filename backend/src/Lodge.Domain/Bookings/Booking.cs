using Lodge.Domain.Apartments;
using Lodge.Domain.Bookings.Events;
using Lodge.Domain.Core.Abstractions;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Shared;

namespace Lodge.Domain.Bookings;

/// <summary>
/// Represents a booking of an apartment by a user.
/// </summary>
/// <remarks>
/// Lifecycle: instant-book listings start as <c>Reserved</c>; other listings start as <c>PendingApproval</c>, which
/// the host accepts (→ <c>Reserved</c>) or declines (→ <c>Rejected</c>), or which <c>Expired</c> after a while.
/// <c>Reserved</c> → (payment) <c>Confirmed</c> → <c>Completed</c>. Pending, reserved and confirmed bookings can be
/// <c>Cancelled</c> by the guest before the stay starts, and confirmed bookings by the host. Cancelling a paid booking
/// computes the refund (see <see cref="RefundPolicy"/>).
/// </remarks>
public sealed class Booking : Entity, IAuditableEntity
{
    public const int MessageToHostMaxLength = 1000;

    /// <summary>
    /// Guests and hosts can review a stay up to this many days after check-out.
    /// </summary>
    public const int ReviewWindowDays = 14;

    private Booking(
        Guid id,
        Guid apartmentId,
        Guid userId,
        DateRange duration,
        int guestCount,
        string? messageToHost,
        BookingStatus status,
        PricingDetails pricing)
        : base(id)
    {
        ApartmentId = apartmentId;
        UserId = userId;
        GuestCount = guestCount;
        MessageToHost = messageToHost;
        Status = status;
        ApplyPricing(duration, pricing);
    }

    private Booking()
    {
    }

    public Guid ApartmentId { get; private set; }

    public Guid UserId { get; private set; }

    public DateRange Duration { get; private set; } = null!;

    public int GuestCount { get; private set; }

    /// <summary>
    /// Gets the message the guest sent to the host with the booking, or null.
    /// </summary>
    public string? MessageToHost { get; private set; }

    /// <summary>
    /// Gets the sum of the nightly prices, before <see cref="Discount"/>.
    /// </summary>
    public Money PriceForPeriod { get; private set; } = null!;

    /// <summary>
    /// Gets the weekly or monthly discount.
    /// </summary>
    public Money Discount { get; private set; } = null!;

    public Money CleaningFee { get; private set; } = null!;

    public Money AmenitiesUpCharge { get; private set; } = null!;

    /// <summary>
    /// Gets the guest service fee (snapshot at reservation).
    /// </summary>
    public Money ServiceFee { get; private set; } = null!;

    /// <summary>
    /// Gets the occupancy tax (snapshot at reservation).
    /// </summary>
    public Money Taxes { get; private set; } = null!;

    /// <summary>
    /// Gets the occupancy tax rate in percent (snapshot at reservation).
    /// </summary>
    public decimal TaxRatePercent { get; private set; }

    public Money TotalPrice { get; private set; } = null!;

    /// <summary>
    /// Gets the listing's cancellation policy at reservation.
    /// </summary>
    public CancellationPolicy CancellationPolicy { get; private set; } = CancellationPolicy.Moderate;

    /// <summary>
    /// Gets the Stripe payment intent id, or a simulated payment id (<c>sim_…</c>), once paid.
    /// </summary>
    public string? PaymentIntentId { get; private set; }

    /// <summary>
    /// Gets the amount paid (in the currency of <see cref="TotalPrice"/>), once paid.
    /// </summary>
    public decimal? AmountPaid { get; private set; }

    public DateTime? PaidOnUtc { get; private set; }

    /// <summary>
    /// Gets the amount refunded (or to be refunded) after the cancellation of a paid booking, else null.
    /// </summary>
    public decimal? RefundAmount { get; private set; }

    public RefundStatus RefundStatus { get; private set; }

    /// <summary>
    /// Gets the payment provider's refund id once the refund succeeded.
    /// </summary>
    public string? RefundId { get; private set; }

    public DateTime? RefundedOnUtc { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the host cancelled the booking (counted in the Superhost cancellation rate).
    /// </summary>
    public bool CancelledByHost { get; private set; }

    /// <summary>
    /// Gets the last day (inclusive) on which the stay can be reviewed: check-out + <see cref="ReviewWindowDays"/>.
    /// </summary>
    public DateOnly ReviewDeadline => Duration.End.AddDays(ReviewWindowDays);

    public BookingStatus Status { get; private set; }

    /// <summary>
    /// Gets when the booking was accepted: on creation for instant-book listings, otherwise when the host accepted the
    /// request. Null for requests that were never accepted (pending, declined, expired or cancelled by the guest
    /// before the host answered). Used by the Superhost cancellation rate.
    /// </summary>
    public DateTime? AcceptedOnUtc { get; private set; }

    public DateTime? ConfirmedOnUtc { get; private set; }

    public DateTime? RejectedOnUtc { get; private set; }

    public DateTime? CompletedOnUtc { get; private set; }

    public DateTime? CancelledOnUtc { get; private set; }

    public DateTime? ExpiredOnUtc { get; private set; }

    public DateTime CreatedOnUtc { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }

    /// <summary>
    /// Books the apartment for the specified period: <see cref="BookingStatus.Reserved"/> for instant-book listings,
    /// otherwise <see cref="BookingStatus.PendingApproval"/>. Availability (overlap) checks are the caller's
    /// responsibility, and <paramref name="pricing"/> must have been calculated for this apartment and period.
    /// </summary>
    public static Result<Booking> Reserve(
        Apartment apartment,
        Guid userId,
        DateRange duration,
        int guestCount,
        string? messageToHost,
        DateTime utcNow,
        PricingDetails pricing)
    {
        ArgumentNullException.ThrowIfNull(apartment);
        ArgumentNullException.ThrowIfNull(duration);
        ArgumentNullException.ThrowIfNull(pricing);

        if (apartment.IsOwnedBy(userId))
        {
            return Result.Failure<Booking>(BookingErrors.OwnListing);
        }

        if (guestCount < 1 || guestCount > apartment.MaximumGuestCount)
        {
            return Result.Failure<Booking>(BookingErrors.InvalidGuestCount(apartment.MaximumGuestCount));
        }

        Result stayLength = apartment.EnsureStayLength(duration.LengthInDays);

        if (stayLength.IsFailure)
        {
            return Result.Failure<Booking>(stayLength.Error);
        }

        string? message = string.IsNullOrWhiteSpace(messageToHost) ? null : messageToHost.Trim();

        if (message?.Length > MessageToHostMaxLength)
        {
            return Result.Failure<Booking>(BookingErrors.MessageTooLong);
        }

        BookingStatus status = apartment.InstantBook ? BookingStatus.Reserved : BookingStatus.PendingApproval;

        var booking = new Booking(Guid.NewGuid(), apartment.Id, userId, duration, guestCount, message, status, pricing)
        {
            CancellationPolicy = apartment.CancellationPolicy,
            AcceptedOnUtc = status == BookingStatus.Reserved ? utcNow : null,
        };

        booking.RaiseDomainEvent(status == BookingStatus.Reserved
            ? new BookingReservedDomainEvent(booking.Id)
            : new BookingRequestedDomainEvent(booking.Id));

        apartment.LastBookedOnUtc = utcNow;

        return booking;
    }

    /// <summary>
    /// The host accepts a booking request; the guest can then pay.
    /// </summary>
    public Result Accept(DateTime utcNow)
    {
        if (Status != BookingStatus.PendingApproval)
        {
            return Result.Failure(BookingErrors.NotPendingApproval);
        }

        if (DateOnly.FromDateTime(utcNow) > Duration.Start)
        {
            return Result.Failure(BookingErrors.AlreadyStarted);
        }

        Status = BookingStatus.Reserved;
        AcceptedOnUtc = utcNow;

        RaiseDomainEvent(new BookingReservedDomainEvent(Id));
        RaiseDomainEvent(new BookingAcceptedDomainEvent(Id));

        return Result.Success();
    }

    /// <summary>
    /// The host declines a booking request.
    /// </summary>
    public Result Decline(DateTime utcNow)
    {
        if (Status != BookingStatus.PendingApproval)
        {
            return Result.Failure(BookingErrors.NotPendingApproval);
        }

        Status = BookingStatus.Rejected;
        RejectedOnUtc = utcNow;

        RaiseDomainEvent(new BookingRejectedDomainEvent(Id));

        return Result.Success();
    }

    /// <summary>
    /// Expires a booking request the host did not answer in time.
    /// </summary>
    public Result Expire(DateTime utcNow)
    {
        if (Status != BookingStatus.PendingApproval)
        {
            return Result.Failure(BookingErrors.NotPendingApproval);
        }

        Status = BookingStatus.Expired;
        ExpiredOnUtc = utcNow;

        RaiseDomainEvent(new BookingExpiredDomainEvent(Id));

        return Result.Success();
    }

    /// <summary>
    /// Records the payment of a reserved booking and confirms it.
    /// </summary>
    /// <param name="paymentIntentId">The Stripe payment intent id or a simulated payment id.</param>
    /// <param name="amountPaid">The amount paid, in the booking's currency.</param>
    /// <param name="utcNow">The current time.</param>
    public Result ConfirmPayment(string paymentIntentId, decimal amountPaid, DateTime utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(paymentIntentId);

        Result result = Confirm(utcNow);

        if (result.IsFailure)
        {
            return result;
        }

        PaymentIntentId = paymentIntentId;
        AmountPaid = amountPaid;
        PaidOnUtc = utcNow;

        return Result.Success();
    }

    /// <summary>
    /// Confirms a reserved booking without recording a payment (bookings paid before payment tracking existed).
    /// </summary>
    public Result Confirm(DateTime utcNow)
    {
        if (Status != BookingStatus.Reserved)
        {
            return Result.Failure(BookingErrors.NotReserved);
        }

        Status = BookingStatus.Confirmed;
        ConfirmedOnUtc = utcNow;

        RaiseDomainEvent(new BookingConfirmedDomainEvent(Id));

        return Result.Success();
    }

    /// <summary>
    /// Rejects a reserved booking or a booking request (admin).
    /// </summary>
    public Result Reject(DateTime utcNow)
    {
        if (Status is not (BookingStatus.Reserved or BookingStatus.PendingApproval))
        {
            return Result.Failure(BookingErrors.NotReserved);
        }

        Status = BookingStatus.Rejected;
        RejectedOnUtc = utcNow;

        RaiseDomainEvent(new BookingRejectedDomainEvent(Id));

        return Result.Success();
    }

    public Result Complete(DateTime utcNow)
    {
        if (Status != BookingStatus.Confirmed)
        {
            return Result.Failure(BookingErrors.NotConfirmed);
        }

        if (DateOnly.FromDateTime(utcNow) < Duration.End)
        {
            return Result.Failure(BookingErrors.NotEnded);
        }

        Status = BookingStatus.Completed;
        CompletedOnUtc = utcNow;

        RaiseDomainEvent(new BookingCompletedDomainEvent(Id));

        return Result.Success();
    }

    /// <summary>
    /// Checks that the guest can cancel the booking now.
    /// </summary>
    public Result EnsureCancellable(DateTime utcNow)
    {
        if (Status is not (BookingStatus.PendingApproval or BookingStatus.Reserved or BookingStatus.Confirmed))
        {
            return Result.Failure(BookingErrors.NotCancellable);
        }

        if (DateOnly.FromDateTime(utcNow) > Duration.Start)
        {
            return Result.Failure(BookingErrors.AlreadyStarted);
        }

        return Result.Success();
    }

    /// <summary>
    /// The guest cancels the booking before the stay starts. For a paid booking, the refund follows the cancellation
    /// policy (see <see cref="RefundPolicy.Quote(Booking, DateOnly)"/>).
    /// </summary>
    /// <returns>The refund quote, or a failure.</returns>
    public Result<RefundQuote> Cancel(DateTime utcNow)
    {
        Result cancellable = EnsureCancellable(utcNow);

        if (cancellable.IsFailure)
        {
            return Result.Failure<RefundQuote>(cancellable.Error);
        }

        RefundQuote quote = RefundPolicy.Quote(this, DateOnly.FromDateTime(utcNow));

        ApplyCancellation(utcNow, quote, byHost: false);

        return quote;
    }

    /// <summary>
    /// The host cancels a confirmed booking before the stay starts; the guest gets a full refund.
    /// </summary>
    /// <returns>The refund quote, or a failure.</returns>
    public Result<RefundQuote> CancelByHost(DateTime utcNow)
    {
        if (Status != BookingStatus.Confirmed)
        {
            return Result.Failure<RefundQuote>(BookingErrors.NotConfirmed);
        }

        if (DateOnly.FromDateTime(utcNow) > Duration.Start)
        {
            return Result.Failure<RefundQuote>(BookingErrors.AlreadyStarted);
        }

        RefundQuote quote = RefundPolicy.FullRefund(this);

        ApplyCancellation(utcNow, quote, byHost: true);

        return quote;
    }

    /// <summary>
    /// Records the success of a pending refund. Does nothing when no refund is pending.
    /// </summary>
    public void MarkRefundSucceeded(string refundId, DateTime utcNow)
    {
        if (RefundStatus != RefundStatus.Pending)
        {
            return;
        }

        RefundStatus = RefundStatus.Succeeded;
        RefundId = refundId;
        RefundedOnUtc = utcNow;

        RaiseDomainEvent(new BookingRefundSucceededDomainEvent(Id));
    }

    /// <summary>
    /// Records the failure of a pending refund. Does nothing when no refund is pending.
    /// </summary>
    public void MarkRefundFailed()
    {
        if (RefundStatus == RefundStatus.Pending)
        {
            RefundStatus = RefundStatus.Failed;
        }
    }

    /// <summary>
    /// Checks that the stay is completed and that the review window is still open.
    /// </summary>
    public Result EnsureReviewable(DateTime utcNow)
    {
        if (Status != BookingStatus.Completed)
        {
            return Result.Failure(BookingErrors.NotCompleted);
        }

        if (DateOnly.FromDateTime(utcNow) > ReviewDeadline)
        {
            return Result.Failure(BookingErrors.ReviewWindowClosed);
        }

        return Result.Success();
    }

    /// <summary>
    /// Moves a pending or reserved booking to new dates. Availability (overlap) checks are the caller's
    /// responsibility, and <paramref name="pricing"/> must have been calculated for the new period.
    /// </summary>
    public Result ChangeDates(Apartment apartment, DateRange duration, DateTime utcNow, PricingDetails pricing)
    {
        ArgumentNullException.ThrowIfNull(apartment);
        ArgumentNullException.ThrowIfNull(duration);
        ArgumentNullException.ThrowIfNull(pricing);

        if (Status == BookingStatus.Confirmed)
        {
            return Result.Failure(BookingErrors.ChangeRequiresCancellation);
        }

        if (Status is not (BookingStatus.PendingApproval or BookingStatus.Reserved))
        {
            return Result.Failure(BookingErrors.NotModifiable);
        }

        if (duration.Start < DateOnly.FromDateTime(utcNow))
        {
            return Result.Failure(BookingErrors.StartDateInPast);
        }

        Result stayLength = apartment.EnsureStayLength(duration.LengthInDays);

        if (stayLength.IsFailure)
        {
            return stayLength;
        }

        DateRange previous = Duration;

        ApplyPricing(duration, pricing);

        apartment.LastBookedOnUtc = utcNow;

        RaiseDomainEvent(new BookingDatesChangedDomainEvent(Id, previous.Start, previous.End));

        return Result.Success();
    }

    private void ApplyCancellation(DateTime utcNow, RefundQuote quote, bool byHost)
    {
        Status = BookingStatus.Cancelled;
        CancelledOnUtc = utcNow;
        CancelledByHost = byHost;

        if (quote.IsPaid)
        {
            RefundAmount = quote.RefundAmount;
            RefundStatus = quote.RefundAmount <= 0
                ? RefundStatus.None
                : PaymentIntentId is null ? RefundStatus.Manual : RefundStatus.Pending;
        }

        RaiseDomainEvent(new BookingCancelledDomainEvent(Id));

        if (RefundStatus == RefundStatus.Pending)
        {
            RaiseDomainEvent(new BookingRefundRequestedDomainEvent(Id));
        }
    }

    private void ApplyPricing(DateRange duration, PricingDetails pricing)
    {
        Duration = duration;
        // Money and DateRange are mapped as EF Core owned types and owned instances must not be shared between
        // owners (the cleaning fee comes straight from the apartment), so every value is copied.
        PriceForPeriod = pricing.PriceForPeriod with { };
        Discount = pricing.Discount with { };
        CleaningFee = pricing.CleaningFee with { };
        AmenitiesUpCharge = pricing.AmenitiesUpCharge with { };
        ServiceFee = pricing.ServiceFee with { };
        Taxes = pricing.Taxes with { };
        TaxRatePercent = pricing.TaxRatePercent;
        TotalPrice = pricing.TotalPrice with { };

    }
}
