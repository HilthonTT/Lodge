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
    IUserRepository userRepository,
    IApartmentRepository apartmentRepository,
    IBookingRepository bookingRepository,
    ICalendarDayRepository calendarDayRepository,
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

        User? user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure<Guid>(UserErrors.NotFound(request.UserId));
        }

        if (!user.IsEmailVerified)
        {
            return Result.Failure<Guid>(UserErrors.EmailNotVerified);
        }

        Apartment? apartment = await apartmentRepository.GetByIdAsync(request.ApartmentId, cancellationToken);

        if (apartment is not { IsListed: true })
        {
            return Result.Failure<Guid>(ApartmentErrors.NotFound(request.ApartmentId));
        }

        Result<DateRange> durationResult = DateRange.Create(request.StartDate, request.EndDate);

        if (durationResult.IsFailure)
        {
            return Result.Failure<Guid>(durationResult.Error);
        }

        DateRange duration = durationResult.Value;
        DateTime utcNow = dateTimeProvider.UtcNow;

        if (duration.Start < DateOnly.FromDateTime(utcNow))
        {
            return Result.Failure<Guid>(BookingErrors.StartDateInPast);
        }

        if (await IsUnavailableAsync(apartment, duration, cancellationToken))
        {
            return Result.Failure<Guid>(BookingErrors.Overlap);
        }

        Dictionary<DateOnly, decimal> priceOverrides = await calendarDayRepository.GetPriceOverridesAsync(
            apartment.Id,
            duration.Start,
            duration.End,
            cancellationToken);

        Result<Booking> booking = Booking.Reserve(
            apartment,
            request.UserId,
            duration,
            request.GuestCount,
            request.MessageToHost,
            utcNow,
            pricingService.CalculatePrice(apartment, duration, priceOverrides));

        if (booking.IsFailure)
        {
            return Result.Failure<Guid>(booking.Error);
        }

        bookingRepository.Insert(booking.Value);

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

        return booking.Value.Id;
    }

    private async Task<bool> IsUnavailableAsync(Apartment apartment, DateRange duration, CancellationToken cancellationToken) =>
        await bookingRepository.IsOverlappingAsync(apartment, duration, cancellationToken: cancellationToken) ||
        await calendarDayRepository.AnyBlockedAsync(apartment.Id, duration.Start, duration.End, cancellationToken);
}
