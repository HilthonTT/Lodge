using Lodge.Contracts.Common;
using Lodge.Domain.Bookings;

namespace Lodge.Application.Apartments.CalculatePrice;

/// <summary>
/// Maps <see cref="PricingDetails"/> to the wire format.
/// </summary>
internal static class PriceDetailsMapper
{
    public static PriceDetailsResponse ToResponse(PricingDetails details) => new()
    {
        Currency = details.PriceForPeriod.Currency.Code,
        Nights = details.Nights,
        PricePerPeriod = details.PriceForPeriod.Amount,
        AverageNightlyPrice = details.AverageNightlyPrice.Amount,
        DiscountAmount = details.Discount.Amount,
        DiscountType = details.DiscountType switch
        {
            StayDiscountType.Weekly => "weekly",
            StayDiscountType.Monthly => "monthly",
            _ => null,
        },
        CleaningFee = details.CleaningFee.Amount,
        AmenitiesUpCharge = details.AmenitiesUpCharge.Amount,
        TotalPrice = details.TotalPrice.Amount,
    };
}
