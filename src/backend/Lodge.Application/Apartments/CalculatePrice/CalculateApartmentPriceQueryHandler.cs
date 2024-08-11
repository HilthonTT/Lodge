using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Common;
using Lodge.Domain.Apartements;
using Lodge.Domain.Bookings;
using Lodge.Domain.Core.Primitives;

namespace Lodge.Application.Apartments.CalculatePrice;

/// <summary>
/// Represents the <see cref="CalculateApartmentPriceQuery"/> handler.
/// </summary>
/// <param name="apartmentRepository">The apartment repository.</param>
internal sealed class CalculateApartmentPriceQueryHandler(
    IApartmentRepository apartmentRepository,
    PricingService pricingService) : IQueryHandler<CalculateApartmentPriceQuery, PriceDetailsResponse>
{
    /// <inheritdoc />
    public async Task<Result<PriceDetailsResponse>> Handle(
        CalculateApartmentPriceQuery request, 
        CancellationToken cancellationToken)
    {
        Apartment? apartment = await apartmentRepository.GetByIdAsync(request.ApartmentId, cancellationToken);
        if (apartment is null)
        {
            return Result.Failure<PriceDetailsResponse>(ApartmentErrors.NotFound(request.ApartmentId));
        }

        var dateRange = DateRange.Create(request.StartDate, request.EndDate);

        var pricingDetails = pricingService.CalculatePrice(apartment, dateRange);

        return new PriceDetailsResponse
        {
            Currency = pricingDetails.PriceForPeriod.Currency.Code,
            PricePerPeriod = pricingDetails.PriceForPeriod.Amount,
            AmenitiesUpCharge = pricingDetails.AmenitiesUpCharge.Amount,
            CleaningFee = pricingDetails.CleaningFee.Amount,
            TotalPrice = pricingDetails.TotalPrice.Amount,
        };
    }
}
