using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Common;
using Lodge.Domain.Apartments;
using Lodge.Domain.Bookings;
using Lodge.Domain.Core.Primitives;

namespace Lodge.Application.Apartments.CalculatePrice;

/// <summary>
/// Represents the <see cref="CalculateApartmentPriceQuery"/> handler. The minimum/maximum stay is not enforced here
/// (it is when booking), so the breakdown can always be shown.
/// </summary>
internal sealed class CalculateApartmentPriceQueryHandler(
    IApartmentRepository apartmentRepository,
    ICalendarDayRepository calendarDayRepository,
    PricingService pricingService) : IQueryHandler<CalculateApartmentPriceQuery, PriceDetailsResponse>
{
    /// <inheritdoc />
    public async Task<Result<PriceDetailsResponse>> Handle(
        CalculateApartmentPriceQuery request,
        CancellationToken cancellationToken)
    {
        Apartment? apartment = await apartmentRepository.GetByIdAsync(request.ApartmentId, cancellationToken);

        if (apartment is not { IsListed: true })
        {
            return Result.Failure<PriceDetailsResponse>(ApartmentErrors.NotFound(request.ApartmentId));
        }

        Result<DateRange> dateRangeResult = DateRange.Create(request.StartDate, request.EndDate);

        if (dateRangeResult.IsFailure)
        {
            return Result.Failure<PriceDetailsResponse>(dateRangeResult.Error);
        }

        Dictionary<DateOnly, decimal> overrides = await calendarDayRepository.GetPriceOverridesAsync(
            apartment.Id,
            request.StartDate,
            request.EndDate,
            cancellationToken);

        PricingDetails pricingDetails = pricingService.CalculatePrice(apartment, dateRangeResult.Value, overrides);

        return PriceDetailsMapper.ToResponse(pricingDetails);
    }
}
