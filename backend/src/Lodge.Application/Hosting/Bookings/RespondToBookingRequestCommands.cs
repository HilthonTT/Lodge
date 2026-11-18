using Lodge.Application.Abstractions.Messaging;
using Lodge.Domain.Apartments;
using Lodge.Domain.Bookings;
using Lodge.Domain.Core.Primitives;

namespace Lodge.Application.Hosting.Bookings;

/// <summary>
/// The host accepts a booking request: PendingApproval → Reserved (the guest can then pay).
/// </summary>
public sealed record AcceptBookingRequestCommand(Guid BookingId) : ICommand;

/// <summary>
/// The host declines a booking request: PendingApproval → Rejected.
/// </summary>
public sealed record DeclineBookingRequestCommand(Guid BookingId) : ICommand;

internal sealed class AcceptBookingRequestCommandHandler(
    IHostAccess hostAccess,
    IBookingRepository bookingRepository,
    IApartmentRepository apartmentRepository,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<AcceptBookingRequestCommand>
{
    public async Task<Result> Handle(AcceptBookingRequestCommand request, CancellationToken cancellationToken)
    {
        Result<Booking> booking = await HostBookings.GetManagedBookingAsync(
            hostAccess,
            bookingRepository,
            apartmentRepository,
            request.BookingId,
            cancellationToken);

        return booking.IsFailure ? booking : booking.Value.Accept(dateTimeProvider.UtcNow);
    }
}

internal sealed class DeclineBookingRequestCommandHandler(
    IHostAccess hostAccess,
    IBookingRepository bookingRepository,
    IApartmentRepository apartmentRepository,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<DeclineBookingRequestCommand>
{
    public async Task<Result> Handle(DeclineBookingRequestCommand request, CancellationToken cancellationToken)
    {
        Result<Booking> booking = await HostBookings.GetManagedBookingAsync(
            hostAccess,
            bookingRepository,
            apartmentRepository,
            request.BookingId,
            cancellationToken);

        return booking.IsFailure ? booking : booking.Value.Decline(dateTimeProvider.UtcNow);
    }
}

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
