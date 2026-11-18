using System.Data;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Apartments;
using Lodge.Domain.Apartments;
using Lodge.Domain.Core.Primitives;

namespace Lodge.Application.Apartments.GetCalendar;

internal sealed class GetApartmentCalendarQueryHandler(
    IApartmentRepository apartmentRepository,
    IDbConnectionFactory factory,
    IDateTimeProvider dateTimeProvider) : IQueryHandler<GetApartmentCalendarQuery, List<ApartmentCalendarDayResponse>>
{
    public async Task<Result<List<ApartmentCalendarDayResponse>>> Handle(
        GetApartmentCalendarQuery request,
        CancellationToken cancellationToken)
    {
        Apartment? apartment = await apartmentRepository.GetByIdAsync(request.ApartmentId, cancellationToken);

        if (apartment is not { IsListed: true })
        {
            return Result.Failure<List<ApartmentCalendarDayResponse>>(ApartmentErrors.NotFound(request.ApartmentId));
        }

        using IDbConnection connection = await factory.GetOpenConnectionAsync(cancellationToken);

        ApartmentQueries.ListingNights nights =
            await ApartmentQueries.GetNightsAsync(connection, apartment.Id, request.From, request.To);

        DateOnly today = DateOnly.FromDateTime(dateTimeProvider.UtcNow);

        return ApartmentQueries.BuildCalendar(apartment, nights, request.From, request.To, today)
            .Select(night => new ApartmentCalendarDayResponse(night.Date, night.Available, night.Price))
            .ToList();
    }
}
