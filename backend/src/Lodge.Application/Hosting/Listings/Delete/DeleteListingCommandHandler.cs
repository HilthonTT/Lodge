using Lodge.Application.Abstractions.Caching;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Domain.Apartments;
using Lodge.Domain.Bookings;
using Lodge.Domain.Core.Primitives;

namespace Lodge.Application.Hosting.Listings.Delete;

internal sealed class DeleteListingCommandHandler(
    IHostAccess hostAccess,
    IApartmentRepository apartmentRepository,
    IBookingRepository bookingRepository,
    IDateTimeProvider dateTimeProvider,
    ICacheInvalidator cacheInvalidator) : ICommandHandler<DeleteListingCommand>
{
    public async Task<Result> Handle(DeleteListingCommand request, CancellationToken cancellationToken)
    {
        Result<HostCaller> caller = await hostAccess.GetCallerAsync(cancellationToken);

        if (caller.IsFailure)
        {
            return caller;
        }

        Apartment? apartment = await apartmentRepository.GetByIdAsync(request.ApartmentId, cancellationToken);

        if (apartment is null)
        {
            return Result.Failure(ApartmentErrors.NotFound(request.ApartmentId));
        }

        Result access = caller.Value.EnsureCanManage(apartment);

        if (access.IsFailure)
        {
            return access;
        }

        DateTime utcNow = dateTimeProvider.UtcNow;

        if (await bookingRepository.HasUpcomingBookingsAsync(apartment.Id, DateOnly.FromDateTime(utcNow), cancellationToken))
        {
            return Result.Failure(ApartmentErrors.HasUpcomingBookings);
        }

        apartment.Delete(utcNow);

        cacheInvalidator.InvalidateOnCommit(CacheKeys.Apartments.GetById(apartment.Id));

        return Result.Success();
    }
}
