using Lodge.Domain.Bookings;
using Lodge.Domain.Core.Abstractions;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Experiences.Events;

namespace Lodge.Domain.Experiences;

/// <summary>
/// A guest's booking of spots in an <see cref="ExperienceSession"/>.
/// </summary>
/// <remarks>
/// Lifecycle: <c>Reserved</c> (spots held, unpaid) → (payment) <c>Confirmed</c> → <c>Completed</c> once the session
/// has ended. Reserved and confirmed bookings can be <c>Cancelled</c> before the session starts: a paid booking is
/// refunded in full when the guest cancels at least <see cref="Experience.FreeCancellationHours"/> hours before the
/// start (or when the host cancels the session), otherwise not at all.
/// </remarks>
public sealed class ExperienceBooking : Entity, IAuditableEntity
{
    private ExperienceBooking(
        Guid id,
        Experience experience,
        ExperienceSession session,
        Guid guestId,
        int guestCount,
        decimal serviceFeePercent)
        : base(id)
    {
        SessionId = session.Id;
        ExperienceId = experience.Id;
        SessionStartsAtUtc = session.StartsAtUtc;
        GuestId = guestId;
        GuestCount = guestCount;
        Currency = experience.Currency;
        PriceTotal = experience.PriceFor(guestCount);
        ServiceFee = PricingService.Round(PriceTotal * serviceFeePercent / 100m);
        TotalPrice = PriceTotal + ServiceFee;
        Status = ExperienceBookingStatus.Reserved;
    }

    /// <remarks>
    /// Required by EF Core.
    /// </remarks>
    private ExperienceBooking()
    {
    }

    public Guid SessionId { get; private set; }

    public Guid ExperienceId { get; private set; }

    /// <summary>
    /// Gets the session's start (copied, so that the cancellation rule and reminders need no join).
    /// </summary>
    public DateTime SessionStartsAtUtc { get; private set; }

    public Guid GuestId { get; private set; }

    public int GuestCount { get; private set; }

    /// <summary>
    /// Gets the price per guest × the number of guests.
    /// </summary>
    public decimal PriceTotal { get; private set; }

    /// <summary>
    /// Gets the guest service fee (the configured guest fee percentage of <see cref="PriceTotal"/>).
    /// </summary>
    public decimal ServiceFee { get; private set; }

    /// <summary>
    /// Gets the amount the guest pays: <see cref="PriceTotal"/> + <see cref="ServiceFee"/>.
    /// </summary>
    public decimal TotalPrice { get; private set; }

    public string Currency { get; private set; } = "USD";

    public ExperienceBookingStatus Status { get; private set; }

    /// <summary>
    /// Gets the Stripe payment intent id, or a simulated payment id (<c>sim_…</c>), once paid.
    /// </summary>
    public string? PaymentIntentId { get; private set; }

    public decimal? AmountPaid { get; private set; }

    public DateTime? PaidOnUtc { get; private set; }

    public DateTime? ConfirmedOnUtc { get; private set; }

    public decimal? RefundAmount { get; private set; }

    public RefundStatus RefundStatus { get; private set; }

    public string? RefundId { get; private set; }

    public DateTime? RefundedOnUtc { get; private set; }

    public DateTime? CancelledOnUtc { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the host cancelled (the session was cancelled).
    /// </summary>
    public bool CancelledByHost { get; private set; }

    public DateTime? CompletedOnUtc { get; private set; }

    public DateTime CreatedOnUtc { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }

    /// <summary>
    /// Reserves spots of a session: they are held until the booking is paid, cancelled or its hold expires. The caller
    /// must hold a lock on the session row.
    /// </summary>
    public static Result<ExperienceBooking> Reserve(
        Experience experience,
        ExperienceSession session,
        Guid guestId,
        int guestCount,
        decimal serviceFeePercent,
        DateTime utcNow)
    {
        ArgumentNullException.ThrowIfNull(experience);
        ArgumentNullException.ThrowIfNull(session);

        if (experience.IsHostedBy(guestId))
        {
            return Result.Failure<ExperienceBooking>(ExperienceBookingErrors.OwnExperience);
        }

        if (guestCount < 1 || guestCount > experience.MaxGuests)
        {
            return Result.Failure<ExperienceBooking>(ExperienceBookingErrors.InvalidGuestCount);
        }

        Result held = session.Hold(guestCount, utcNow);

        if (held.IsFailure)
        {
            return Result.Failure<ExperienceBooking>(held.Error);
        }

        return new ExperienceBooking(Guid.NewGuid(), experience, session, guestId, guestCount, serviceFeePercent);
    }

    /// <summary>
    /// Gets the last moment at which the guest can cancel with a full refund.
    /// </summary>
    public DateTime FreeCancellationUntilUtc => SessionStartsAtUtc.AddHours(-Experience.FreeCancellationHours);

    public bool IsPaid => PaymentIntentId is not null;

    /// <summary>
    /// Records the payment and confirms the booking. Confirmed or completed bookings are left unchanged (payment
    /// providers retry deliveries).
    /// </summary>
    public Result ConfirmPayment(string paymentIntentId, decimal amountPaid, DateTime utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(paymentIntentId);

        if (Status is ExperienceBookingStatus.Confirmed or ExperienceBookingStatus.Completed)
        {
            return Result.Success();
        }

        if (Status != ExperienceBookingStatus.Reserved)
        {
            return Result.Failure(ExperienceBookingErrors.NotReserved);
        }

        Status = ExperienceBookingStatus.Confirmed;
        ConfirmedOnUtc = utcNow;
        PaymentIntentId = paymentIntentId;
        AmountPaid = amountPaid;
        PaidOnUtc = utcNow;

        RaiseDomainEvent(new ExperienceBookingConfirmedDomainEvent(Id));

        return Result.Success();
    }

    /// <summary>
    /// Gets the refund the guest would get when cancelling now.
    /// </summary>
    public decimal QuoteRefund(DateTime utcNow, bool byHost) =>
        AmountPaid is decimal paid && (byHost || utcNow <= FreeCancellationUntilUtc) ? paid : 0m;

    /// <summary>
    /// Cancels the booking before the session starts and releases its spots. Returns the refund amount.
    /// </summary>
    public Result<decimal> Cancel(ExperienceSession session, DateTime utcNow, bool byHost)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (Status is not (ExperienceBookingStatus.Reserved or ExperienceBookingStatus.Confirmed) ||
            (!byHost && SessionStartsAtUtc <= utcNow))
        {
            return Result.Failure<decimal>(ExperienceBookingErrors.NotCancellable);
        }

        decimal refund = QuoteRefund(utcNow, byHost);

        Status = ExperienceBookingStatus.Cancelled;
        CancelledOnUtc = utcNow;
        CancelledByHost = byHost;
        session.Release(GuestCount);

        if (IsPaid)
        {
            RefundAmount = refund;
            RefundStatus = refund <= 0 ? RefundStatus.None : RefundStatus.Pending;
        }

        RaiseDomainEvent(new ExperienceBookingCancelledDomainEvent(Id));

        if (RefundStatus == RefundStatus.Pending)
        {
            RaiseDomainEvent(new ExperienceBookingRefundRequestedDomainEvent(Id));
        }

        return refund;
    }

    /// <summary>
    /// Releases the spots of an unpaid booking whose payment hold expired (no notification).
    /// </summary>
    public bool ExpireHold(ExperienceSession session, DateTime utcNow)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (Status != ExperienceBookingStatus.Reserved)
        {
            return false;
        }

        Status = ExperienceBookingStatus.Cancelled;
        CancelledOnUtc = utcNow;
        session.Release(GuestCount);

        return true;
    }

    public Result Complete(DateTime utcNow, int durationMinutes)
    {
        if (Status != ExperienceBookingStatus.Confirmed || SessionStartsAtUtc.AddMinutes(durationMinutes) > utcNow)
        {
            return Result.Failure(ExperienceBookingErrors.NotCompleted);
        }

        Status = ExperienceBookingStatus.Completed;
        CompletedOnUtc = utcNow;

        return Result.Success();
    }

    public void MarkRefundSucceeded(string refundId, DateTime utcNow)
    {
        if (RefundStatus != RefundStatus.Pending)
        {
            return;
        }

        RefundStatus = RefundStatus.Succeeded;
        RefundId = refundId;
        RefundedOnUtc = utcNow;

        RaiseDomainEvent(new ExperienceBookingRefundSucceededDomainEvent(Id));
    }

    public void MarkRefundFailed()
    {
        if (RefundStatus == RefundStatus.Pending)
        {
            RefundStatus = RefundStatus.Failed;
        }
    }
}
