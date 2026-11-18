using Lodge.Domain.Apartments;
using Lodge.Domain.Bookings;
using Lodge.Domain.Core.Primitives;

namespace Lodge.Application.Hosting.Bookings;

internal static class HostBookings
{
    /// <summary>
    /// Loads a booking of one of the caller's listings (admins may manage every booking).
    /// </summary>
    public static async Task<Result<Booking>> GetManagedBookingAsync(
        IHostAccess hostAccess,
        IBookingRepository bookingRepository,
        IApartmentRepository apartmentRepository,
        Guid bookingId,
        CancellationToken cancellationToken)
    {
        Result<HostCaller> caller = await hostAccess.GetCallerAsync(cancellationToken);

        if (caller.IsFailure)
        {
            return Result.Failure<Booking>(caller.Error);
        }

        Booking? booking = await bookingRepository.GetByIdAsync(bookingId, cancellationToken);

        if (booking is null)
        {
            return Result.Failure<Booking>(BookingErrors.NotFound(bookingId));
        }

        Apartment? apartment = await apartmentRepository.GetByIdAsync(booking.ApartmentId, cancellationToken);

        if (apartment is null)
        {
            return Result.Failure<Booking>(BookingErrors.NotFound(bookingId));
        }

        Result access = caller.Value.EnsureCanManage(apartment);

        return access.IsFailure ? Result.Failure<Booking>(access.Error) : booking;
    }
}
