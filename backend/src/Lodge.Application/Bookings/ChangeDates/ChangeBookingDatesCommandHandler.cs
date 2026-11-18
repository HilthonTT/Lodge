using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Application.Core.Exceptions;
using Lodge.Application.Payments;
using Lodge.Domain.Apartments;
using Lodge.Domain.Bookings;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Users;

namespace Lodge.Application.Bookings.ChangeDates;

internal sealed class ChangeBookingDatesCommandHandler(
    IUserIdentifierProvider userIdentifierProvider,
    IBookingRepository bookingRepository,
    IApartmentRepository apartmentRepository,
    ICalendarDayRepository calendarDayRepository,
    IUnitOfWork unitOfWork,
    BookingPricing bookingPricing,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<ChangeBookingDatesCommand>
{
    public async Task<Result> Handle(ChangeBookingDatesCommand request, CancellationToken cancellationToken)
    {
        Booking? booking = await bookingRepository.GetByIdAsync(request.BookingId, cancellationToken);

        if (booking is null)
        {
            return Result.Failure(BookingErrors.NotFound(request.BookingId));
        }

        if (booking.UserId != userIdentifierProvider.UserId)
        {
            return Result.Failure(UserErrors.InvalidPermissions);
        }

        if (booking.Status == BookingStatus.Confirmed)
        {
            return Result.Failure(BookingErrors.ChangeRequiresCancellation);
        }

        if (booking.Status is not (BookingStatus.PendingApproval or BookingStatus.Reserved))
        {
            return Result.Failure(BookingErrors.NotModifiable);
        }

        Apartment? apartment = await apartmentRepository.GetByIdAsync(booking.ApartmentId, cancellationToken);

        if (apartment is null)
        {
            return Result.Failure(ApartmentErrors.NotFound(booking.ApartmentId));
        }

        Result<DateRange> durationResult = DateRange.Create(request.StartDate, request.EndDate);

        if (durationResult.IsFailure)
        {
            return durationResult;
        }

        DateRange duration = durationResult.Value;
        DateTime utcNow = dateTimeProvider.UtcNow;

        if (duration.Start < DateOnly.FromDateTime(utcNow))
        {
            return Result.Failure(BookingErrors.StartDateInPast);
        }

        Result stayLength = apartment.EnsureStayLength(duration.LengthInDays);

        if (stayLength.IsFailure)
        {
            return stayLength;
        }

        if (await bookingRepository.IsOverlappingAsync(apartment, duration, booking.Id, cancellationToken) ||
            await calendarDayRepository.AnyBlockedAsync(apartment.Id, duration.Start, duration.End, cancellationToken))
        {
            return Result.Failure(BookingErrors.Overlap);
        }

        Dictionary<DateOnly, decimal> priceOverrides = await calendarDayRepository.GetPriceOverridesAsync(
            apartment.Id,
            duration.Start,
            duration.End,
            cancellationToken);

        Result result = booking.ChangeDates(
            apartment,
            duration,
            utcNow,
            bookingPricing.Calculate(apartment, duration, priceOverrides));

        if (result.IsFailure)
        {
            return result;
        }

        try
        {
            // Bumps the apartment's concurrency token, like a new reservation.
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyException)
        {
            return Result.Failure(BookingErrors.Overlap);
        }

        return Result.Success();
    }
}
