using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Application.Core.Exceptions;
using Lodge.Domain.Apartments;
using Lodge.Domain.Bookings;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Users;

namespace Lodge.Application.Bookings.Reserve;

internal sealed class ReserveBookingCommandHandler(
    IUserIdentifierProvider userIdentifierProvider,
    IApartmentRepository apartmentRepository,
    IBookingRepository bookingRepository,
    IUnitOfWork unitOfWork,
    PricingService pricingService,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<ReserveBookingCommand, Guid>
{
    public async Task<Result<Guid>> Handle(ReserveBookingCommand request, CancellationToken cancellationToken)
    {
        if (request.UserId != userIdentifierProvider.UserId)
        {
            return Result.Failure<Guid>(UserErrors.InvalidPermissions);
        }

        Apartment? apartment = await apartmentRepository.GetByIdAsync(request.ApartmentId, cancellationToken);

        if (apartment is null)
        {
            return Result.Failure<Guid>(ApartmentErrors.NotFound(request.ApartmentId));
        }

        Result<DateRange> durationResult = DateRange.Create(request.StartDate, request.EndDate);

        if (durationResult.IsFailure)
        {
            return Result.Failure<Guid>(durationResult.Error);
        }

        DateTime utcNow = dateTimeProvider.UtcNow;

        if (durationResult.Value.Start < DateOnly.FromDateTime(utcNow))
        {
            return Result.Failure<Guid>(BookingErrors.StartDateInPast);
        }

        if (await bookingRepository.IsOverlappingAsync(apartment, durationResult.Value, cancellationToken))
        {
            return Result.Failure<Guid>(BookingErrors.Overlap);
        }

        var booking = Booking.Reserve(apartment, request.UserId, durationResult.Value, utcNow, pricingService);

        bookingRepository.Insert(booking);

        try
        {
            // Saving bumps the apartment's concurrency token (LastBookedOnUtc / xmin), so two concurrent
            // reservations for the same apartment cannot both pass the overlap check.
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyException)
        {
            return Result.Failure<Guid>(BookingErrors.Overlap);
        }

        return booking.Id;
    }
}
