using Lodge.Domain.Apartments;
using Lodge.Domain.Shared;

namespace Lodge.Domain.Bookings;

/// <summary>
/// Calculates the price of a stay.
/// </summary>
/// <remarks>
/// <list type="number">
/// <item>Each night costs the host's override for that date, else the weekend price (Friday and Saturday nights),
/// else the base price (<see cref="Apartment.GetNightlyPrice"/>).</item>
/// <item>Stays of 28+ nights get the monthly discount, stays of 7+ nights the weekly discount (only the larger
/// applicable one, and only when it is set).</item>
/// <item>The amenity up-charge is a percentage of the discounted nights price.</item>
/// <item>The cleaning fee is added once.</item>
/// </list>
/// Every amount is rounded to cents.
/// </remarks>
public sealed class PricingService
{
    /// <summary>
    /// Calculates the price breakdown of staying in <paramref name="apartment"/> for <paramref name="period"/>.
    /// </summary>
    /// <param name="apartment">The apartment being booked.</param>
    /// <param name="period">The stay (check-in to check-out).</param>
    /// <param name="nightlyPriceOverrides">The host's custom prices by night, if any.</param>
    public PricingDetails CalculatePrice(
        Apartment apartment,
        DateRange period,
        IReadOnlyDictionary<DateOnly, decimal>? nightlyPriceOverrides = null)
    {
        ArgumentNullException.ThrowIfNull(apartment);
        ArgumentNullException.ThrowIfNull(period);

        Currency currency = apartment.Price.Currency;
        int nights = period.LengthInDays;

        decimal nightsTotal = 0;

        for (int night = 0; night < nights; night++)
        {
            DateOnly date = period.Start.AddDays(night);
            decimal? overridePrice = nightlyPriceOverrides is not null &&
                                     nightlyPriceOverrides.TryGetValue(date, out decimal price)
                ? price
                : null;

            nightsTotal += apartment.GetNightlyPrice(date, overridePrice);
        }

        (StayDiscountType discountType, int discountPercent) = GetDiscount(apartment, nights);

        decimal discount = Round(nightsTotal * discountPercent / 100m);
        decimal discountedNights = nightsTotal - discount;

        decimal upChargePercentage = apartment.Amenities.Sum(GetUpChargePercentage);
        decimal amenitiesUpCharge = Round(discountedNights * upChargePercentage);

        decimal total = discountedNights + amenitiesUpCharge + apartment.CleaningFee.Amount;

        return new PricingDetails(
            nights,
            new Money(Round(nightsTotal), currency),
            new Money(discount, currency),
            discountType,
            new Money(apartment.CleaningFee.Amount, currency),
            new Money(amenitiesUpCharge, currency),
            new Money(Round(total), currency));
    }

    private static (StayDiscountType Type, int Percent) GetDiscount(Apartment apartment, int nights)
    {
        if (nights >= Apartment.MonthlyDiscountMinNights && apartment.MonthlyDiscountPercent > 0)
        {
            return (StayDiscountType.Monthly, apartment.MonthlyDiscountPercent);
        }

        if (nights >= Apartment.WeeklyDiscountMinNights && apartment.WeeklyDiscountPercent > 0)
        {
            return (StayDiscountType.Weekly, apartment.WeeklyDiscountPercent);
        }

        return (StayDiscountType.None, 0);
    }

    private static decimal GetUpChargePercentage(Amenity amenity) => amenity switch
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
        _ => 0,
    };

    private static decimal Round(decimal amount) => Math.Round(amount, 2, MidpointRounding.AwayFromZero);
}
