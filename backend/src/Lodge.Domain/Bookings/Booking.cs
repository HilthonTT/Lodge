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
/// Lifecycle: <c>Reserved</c> → (payment) <c>Confirmed</c> → <c>Completed</c>.
/// A reserved booking can also be <c>Rejected</c>, and a reserved or confirmed booking
/// can be <c>Cancelled</c> by the guest before the stay starts.
/// </remarks>
public sealed class Booking : Entity, IAuditableEntity
{
    private Booking(
       Guid id,
       Guid apartmentId,
       Guid userId,
       DateRange duration,
       PricingDetails pricing)
       : base(id)
    {
        ApartmentId = apartmentId;
        UserId = userId;
        Duration = duration;
        // Money is mapped as an EF Core owned type and owned instances must not be shared between owners
        // (the cleaning fee comes straight from the apartment), so every amount is copied.
        PriceForPeriod = pricing.PriceForPeriod with { };
        CleaningFee = pricing.CleaningFee with { };
        AmenitiesUpCharge = pricing.AmenitiesUpCharge with { };
        TotalPrice = pricing.TotalPrice with { };
        Status = BookingStatus.Reserved;
    }

    private Booking()
    {
    }

    public Guid ApartmentId { get; private set; }

    public Guid UserId { get; private set; }

    public DateRange Duration { get; private set; } = null!;

    public Money PriceForPeriod { get; private set; } = null!;

    public Money CleaningFee { get; private set; } = null!;

    public Money AmenitiesUpCharge { get; private set; } = null!;

    public Money TotalPrice { get; private set; } = null!;

    public BookingStatus Status { get; private set; }

    public DateTime? ConfirmedOnUtc { get; private set; }

    public DateTime? RejectedOnUtc { get; private set; }

    public DateTime? CompletedOnUtc { get; private set; }

    public DateTime? CancelledOnUtc { get; private set; }

    public DateTime CreatedOnUtc { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }

    /// <summary>
    /// Reserves the apartment for the specified period. Overlap checks are the caller's responsibility.
    /// </summary>
    public static Booking Reserve(
       Apartment apartment,
       Guid userId,
       DateRange duration,
       DateTime utcNow,
       PricingService pricingService)
    {
        PricingDetails pricingDetails = pricingService.CalculatePrice(apartment, duration);

        var booking = new Booking(Guid.NewGuid(), apartment.Id, userId, duration, pricingDetails);

        booking.RaiseDomainEvent(new BookingReservedDomainEvent(booking.Id));

        apartment.LastBookedOnUtc = utcNow;

        return booking;
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

    public Result Reject(DateTime utcNow)
    {
        if (Status != BookingStatus.Reserved)
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
        if (Status is not (BookingStatus.Reserved or BookingStatus.Confirmed))
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
}
