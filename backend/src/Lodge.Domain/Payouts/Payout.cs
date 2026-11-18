using Lodge.Domain.Bookings;
using Lodge.Domain.Core.Abstractions;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Experiences;
using Lodge.Domain.Payouts.Events;
using Lodge.Domain.Shared;

namespace Lodge.Domain.Payouts;

/// <summary>
/// The payout of a confirmed booking to its host.
/// </summary>
/// <remarks>
/// Created when the booking is confirmed (<see cref="PayoutStatus.Scheduled"/> for check-in + 1 day) with the stay
/// subtotal (nights - discount + amenity up-charge + cleaning fee) minus the host service fee; the guest service fee
/// and the taxes are not paid out. A daily job pays due payouts (<see cref="PayoutStatus.Paid"/>), or puts them
/// <see cref="PayoutStatus.OnHold"/> while the host has no active payout account. Cancellations reduce the payout to
/// the non-refunded part of the stay, or cancel it.
/// </remarks>
public sealed class Payout : Entity, IAuditableEntity
{
    /// <summary>
    /// Payouts are scheduled this many days after check-in.
    /// </summary>
    public const int DaysAfterCheckIn = 1;

    private Payout(
        Guid id,
        Guid? bookingId,
        Guid? experienceBookingId,
        Guid hostId,
        Money amount,
        decimal hostFeePercent,
        DateOnly scheduledFor)
        : base(id)
    {
        BookingId = bookingId;
        ExperienceBookingId = experienceBookingId;
        HostId = hostId;
        Amount = amount;
        HostFeePercent = hostFeePercent;
        ScheduledFor = scheduledFor;
        Status = PayoutStatus.Scheduled;
    }

    /// <remarks>
    /// Required by EF Core.
    /// </remarks>
    private Payout()
    {
    }

    /// <summary>
    /// Gets the paid stay, or null for the payout of an experience booking.
    /// </summary>
    public Guid? BookingId { get; private set; }

    /// <summary>
    /// Gets the paid experience booking, or null for the payout of a stay.
    /// </summary>
    public Guid? ExperienceBookingId { get; private set; }

    public Guid HostId { get; private set; }

    public Money Amount { get; private set; } = null!;

    /// <summary>
    /// Gets the host service fee percentage deducted from <see cref="Amount"/>.
    /// </summary>
    public decimal HostFeePercent { get; private set; }

    public PayoutStatus Status { get; private set; }

    public DateOnly ScheduledFor { get; private set; }

    public DateTime? PaidOnUtc { get; private set; }

    /// <summary>
    /// Gets the Stripe transfer id or a simulated transfer id (sim_tr_...), once paid.
    /// </summary>
    public string? TransferId { get; private set; }

    public string? FailureReason { get; private set; }

    public DateTime CreatedOnUtc { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }

    /// <summary>
    /// Schedules the payout of a confirmed booking.
    /// </summary>
    public static Payout Schedule(Booking booking, Guid hostId, decimal hostFeePercent)
    {
        ArgumentNullException.ThrowIfNull(booking);

        decimal subtotal = booking.PriceForPeriod.Amount - booking.Discount.Amount +
                           booking.AmenitiesUpCharge.Amount + booking.CleaningFee.Amount;

        return new Payout(
            Guid.NewGuid(),
            booking.Id,
            null,
            hostId,
            new Money(HostFees.Payout(subtotal, hostFeePercent), booking.TotalPrice.Currency),
            hostFeePercent,
            booking.Duration.Start.AddDays(DaysAfterCheckIn));
    }

    /// <summary>
    /// Schedules the payout of a paid experience booking for the day after the session: the price of the spots minus
    /// the host service fee (the guest service fee is not paid out).
    /// </summary>
    public static Payout ScheduleExperience(ExperienceBooking booking, Guid hostId, decimal hostFeePercent)
    {
        ArgumentNullException.ThrowIfNull(booking);

        return new Payout(
            Guid.NewGuid(),
            null,
            booking.Id,
            hostId,
            new Money(HostFees.Payout(booking.PriceTotal, hostFeePercent), Currency.FromCode(booking.Currency)),
            hostFeePercent,
            DateOnly.FromDateTime(booking.SessionStartsAtUtc).AddDays(DaysAfterCheckIn));
    }

    /// <summary>
    /// Gets a value indicating whether the payout is due: scheduled or on hold, and its date has come.
    /// </summary>
    public bool IsDue(DateOnly today) =>
        Status is PayoutStatus.Scheduled or PayoutStatus.OnHold && ScheduledFor <= today;

    public Result MarkPaid(string transferId, DateTime utcNow)
    {
        if (Status is not (PayoutStatus.Scheduled or PayoutStatus.OnHold))
        {
            return Result.Failure(PayoutErrors.NotPayable);
        }

        Status = PayoutStatus.Paid;
        TransferId = transferId;
        PaidOnUtc = utcNow;
        FailureReason = null;

        RaiseDomainEvent(new PayoutPaidDomainEvent(Id));

        return Result.Success();
    }

    /// <summary>
    /// Holds a scheduled payout until the host connects a payout account.
    /// </summary>
    public void PutOnHold()
    {
        if (Status == PayoutStatus.Scheduled)
        {
            Status = PayoutStatus.OnHold;
        }
    }

    public void MarkFailed(string reason)
    {
        ArgumentNullException.ThrowIfNull(reason);

        if (Status is PayoutStatus.Scheduled or PayoutStatus.OnHold)
        {
            Status = PayoutStatus.Failed;
            FailureReason = reason.Length > 500 ? reason[..500] : reason;
        }
    }

    /// <summary>
    /// Adjusts an unpaid payout after the booking was cancelled: the host keeps the non-refunded part of the stay
    /// (<paramref name="retainedStayAmount"/>) minus the host fee; nothing left cancels the payout.
    /// </summary>
    public void AdjustForCancellation(decimal retainedStayAmount)
    {
        if (Status is PayoutStatus.Paid or PayoutStatus.Cancelled)
        {
            return;
        }

        decimal amount = retainedStayAmount <= 0 ? 0 : HostFees.Payout(retainedStayAmount, HostFeePercent);

        Amount = new Money(amount, Amount.Currency);

        if (amount <= 0)
        {
            Status = PayoutStatus.Cancelled;
        }
    }
}
