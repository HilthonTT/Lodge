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
/// <c>Cancelled</c> by the guest before the stay starts.
/// </remarks>
public sealed class Booking : Entity, IAuditableEntity
{
    public const int MessageToHostMaxLength = 1000;

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

    public Money TotalPrice { get; private set; } = null!;

    public BookingStatus Status { get; private set; }

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

        var booking = new Booking(Guid.NewGuid(), apartment.Id, userId, duration, guestCount, message, status, pricing);

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

        RaiseDomainEvent(new BookingReservedDomainEvent(Id));

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

    public Result Cancel(DateTime utcNow)
    {
        if (Status is not (BookingStatus.PendingApproval or BookingStatus.Reserved or BookingStatus.Confirmed))
        {
            return Result.Failure(BookingErrors.NotCancellable);
        }

        if (DateOnly.FromDateTime(utcNow) > Duration.Start)
        {
            return Result.Failure(BookingErrors.AlreadyStarted);
        }

        Status = BookingStatus.Cancelled;
        CancelledOnUtc = utcNow;

        RaiseDomainEvent(new BookingCancelledDomainEvent(Id));

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

        ApplyPricing(duration, pricing);

        apartment.LastBookedOnUtc = utcNow;

        return Result.Success();
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
        TotalPrice = pricing.TotalPrice with { };
    }
}
