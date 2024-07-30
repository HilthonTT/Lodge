using Lodge.Domain.Apartements;
using Lodge.Domain.Bookings.Events;
using Lodge.Domain.Core.Abstractions;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Shared;

namespace Lodge.Domain.Bookings;

/// <summary>
/// Represents the booking entity.
/// </summary>
public sealed class Booking : Entity, IAuditableEntity
{
    /// <summary>
    /// Initializes a new instance of <see cref="Booking"/> class.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="apartmentId"></param>
    /// <param name="userId"></param>
    /// <param name="duration"></param>
    /// <param name="priceForPeriod"></param>
    /// <param name="cleaningFee"></param>
    /// <param name="amenitiesUpCharge"></param>
    /// <param name="totalPrice"></param>
    /// <param name="status"></param>
    private Booking(
       Guid id,
       Guid apartmentId,
       Guid userId,
       DateRange duration,
       Money priceForPeriod,
       Money cleaningFee,
       Money amenitiesUpCharge,
       Money totalPrice,
       BookingStatus status)
       : base(id)
    {
        ApartmentId = apartmentId;
        UserId = userId;
        Duration = duration;
        PriceForPeriod = priceForPeriod;
        CleaningFee = cleaningFee;
        AmenitiesUpCharge = amenitiesUpCharge;
        TotalPrice = totalPrice;
        Status = status;
    }

    /// <summary>
    /// Initializes a new blank instance of the <see cref="Booking"/> class.
    /// </summary>
    /// <remarks>
    /// Required by EF Core.
    /// </remarks>
    private Booking()
    {
    }

    public Guid ApartmentId { get; private set; }

    public Guid UserId { get; private set; }

    public DateRange Duration { get; private set; }

    public Money PriceForPeriod { get; private set; }

    public Money CleaningFee { get; private set; }

    public Money AmenitiesUpCharge { get; private set; }

    public Money TotalPrice { get; private set; }

    public BookingStatus Status { get; private set; }

    public DateTime? ConfirmedOnUtc { get; private set; }

    public DateTime? RejectedOnUtc { get; private set; }

    public DateTime? CompletedOnUtc { get; private set; }

    public DateTime? CancelledOnUtc { get; private set; }

    public DateTime CreatedOnUtc { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }

    /// <summary>
    /// Reserves a booking, creates a new booking.
    /// </summary>
    /// <param name="apartment">The apartment to reserve.</param>
    /// <param name="userId">The user's id that is reserving.</param>
    /// <param name="duration">The duration.</param>
    /// <param name="pricingService">The pricing service.</param>
    /// <param name="utcNow">The UTC time of the apartment's last booked date time.</param>
    /// <returns>The newly created booking instance.</returns>
    public static Booking Reserve(
       Apartment apartment,
       Guid userId,
       DateRange duration,
       DateTime utcNow,
       PricingService pricingService)
    {
        var pricingDetails = pricingService.CalculatePrice(apartment, duration);

        var booking = new Booking(
            Guid.NewGuid(),
            apartment.Id,
            userId,
            duration,
            pricingDetails.PriceForPeriod,
            pricingDetails.CleaningFee,
            pricingDetails.AmenitiesUpCharge,
            pricingDetails.TotalPrice,
            BookingStatus.Reserved);

        booking.RaiseDomainEvent(new BookingReservedDomainEvent(booking.Id));

        apartment.LastBookedOnUtc = utcNow;

        return booking;
    }

    /// <summary>
    /// Confirms a booking.
    /// </summary>
    /// <param name="utcNow">The UTC time of its confirmed datetime.</param>
    /// <returns>A result.</returns>
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
    /// Rejects a booking.
    /// </summary>
    /// <param name="utcNow">The UTC Time of its rejected datetime.</param>
    /// <returns>A result.</returns>

    public Result Reject(DateTime utcNow)
    {
        if (Status != BookingStatus.Reserved)
        {
            return Result.Failure(BookingErrors.NotReserved);
        }

        Status = BookingStatus.Rejected;
        ConfirmedOnUtc = utcNow;

        RaiseDomainEvent(new BookingRejectedDomainEvent(Id));

        return Result.Success();
    }

    /// <summary>
    /// Completes a booking.
    /// </summary>
    /// <param name="utcNow">The UTC Time of its completed datetime.</param>
    /// <returns>A result</returns>
    public Result Complete(DateTime utcNow)
    {
        if (Status != BookingStatus.Confirmed)
        {
            return Result.Failure(BookingErrors.NotConfirmed);
        }

        Status = BookingStatus.Completed;
        ConfirmedOnUtc = utcNow;

        RaiseDomainEvent(new BookingCompletedDomainEvent(Id));

        return Result.Success();
    }

    /// <summary>
    /// Cancels a booking.
    /// </summary>
    /// <param name="utcNow">The UTC Time of its cancelled datetime</param>
    /// <returns>A result</returns>
    public Result Cancel(DateTime utcNow)
    {
        if (Status != BookingStatus.Confirmed)
        {
            return Result.Failure(BookingErrors.NotConfirmed);
        }

        var currentDate = DateOnly.FromDateTime(utcNow);

        if (currentDate > Duration.Start)
        {
            return Result.Failure(BookingErrors.AlreadyStarted);
        }

        Status = BookingStatus.Cancelled;
        ConfirmedOnUtc = utcNow;

        RaiseDomainEvent(new BookingCancelledDomainEvent(Id));

        return Result.Success();
    }
}
