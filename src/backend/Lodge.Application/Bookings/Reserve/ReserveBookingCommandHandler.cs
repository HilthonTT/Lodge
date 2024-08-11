using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Application.Core.Exceptions;
using Lodge.Domain.Apartements;
using Lodge.Domain.Bookings;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Users;
using MediatR;

namespace Lodge.Application.Bookings.Reserve;

/// <summary>
/// Represents the <see cref="ReserveBookingCommand"/> handler.
/// </summary>
/// <param name="userIdentifierProvider">The user identifier provider.</param>
/// <param name="apartmentRepository">The apartment repository.</param>
/// <param name="bookingRepository">The booking repository.</param>
/// <param name="unitOfWork">The unit of work.</param>
/// <param name="pricingService">The pricing service.</param>
/// <param name="dateTimeProvider">The date time provider.</param>
internal sealed class ReserveBookingCommandHandler(
    IUserIdentifierProvider userIdentifierProvider,
    IApartmentRepository apartmentRepository,
    IBookingRepository bookingRepository,
    IUnitOfWork unitOfWork,
    PricingService pricingService,
    IDateTimeProvider dateTimeProvider,
    IPublisher publisher) : ICommandHandler<ReserveBookingCommand, Guid>
{
    /// <inheritdoc />
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

        if (await bookingRepository.IsOverlappingAsync(apartment, durationResult.Value, cancellationToken))
        {
            return Result.Failure<Guid>(BookingErrors.Overlap);
        }

        try
        {
            var booking = Booking.Reserve(
                apartment, 
                userIdentifierProvider.UserId,
                durationResult.Value, 
                dateTimeProvider.UtcNow, 
                pricingService);

            bookingRepository.Insert(booking);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            await publisher.Publish(new BookingReservedEvent(booking.Id), cancellationToken);

            return booking.Id;
        }
        catch (ConcurrencyException)
        {
            return Result.Failure<Guid>(BookingErrors.Overlap);
        }
    }
}
