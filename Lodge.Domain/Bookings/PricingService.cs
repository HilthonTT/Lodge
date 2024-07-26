using Lodge.Domain.Apartements;
using Lodge.Domain.Shared;

namespace Lodge.Domain.Bookings;

/// <summary>
/// Provides methods to calculate pricing details for apartments.
/// </summary>
public sealed class PricingService
{
    /// <summary>
    /// Calculates the total price for an apartment booking over a specified period, including amenities upcharges and cleaning fees.
    /// </summary>
    /// <param name="apartment">The apartment being booked.</param>
    /// <param name="period">The date range of the booking.</param>
    /// <returns>A <see cref="PricingDetails"/> object containing the price breakdown for the booking.</returns>
    public PricingDetails CalculatePrice(Apartment apartment, DateRange period)
    {
        var currency = apartment.Price.Currency;

        var priceForPeriod = new Money(apartment.Price.Amount * period.LengthInDays, currency);

        decimal percentageUpCharge = 0;

        foreach (var amenity in apartment.Amenities)
        {
            percentageUpCharge += amenity switch
            {
                Amenity.GardenView or Amenity.MountainView => 0.05m,
                Amenity.AirConditioning => 0.01m,
                Amenity.Parking => 0.01m,
                Amenity.WiFi => 0.02m,
                Amenity.PetFriendly => 0.03m,
                Amenity.SwimmingPool => 0.04m,
                Amenity.Gym => 0.03m,
                Amenity.Spa => 0.06m,
                Amenity.Terrace => 0.02m,
                _ => 0
            };
        }

        var amenitiesUpCharge = Money.Zero(currency);
        if (percentageUpCharge > 0)
        {
            amenitiesUpCharge = new Money(priceForPeriod.Amount * percentageUpCharge, currency);
        }

        var totalPrice = Money.Zero();
        totalPrice += priceForPeriod;

        if (!apartment.CleaningFee.IsZero())
        {
            totalPrice += apartment.CleaningFee;
        }

        totalPrice += amenitiesUpCharge;

        return new PricingDetails(priceForPeriod, apartment.CleaningFee, amenitiesUpCharge, totalPrice);
    }
}
