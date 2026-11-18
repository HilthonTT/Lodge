using Lodge.Domain.Apartments;
using Lodge.Domain.Bookings;
using Microsoft.Extensions.Options;

namespace Lodge.Application.Payments;

/// <summary>
/// Prices stays with the configured guest service fee and the occupancy tax of the listing country.
/// </summary>
public sealed class BookingPricing(PricingService pricingService, IOptions<PaymentsOptions> options)
{
    public PricingDetails Calculate(
        Apartment apartment,
        DateRange period,
        IReadOnlyDictionary<DateOnly, decimal>? nightlyPriceOverrides = null)
    {
        ArgumentNullException.ThrowIfNull(apartment);

        return pricingService.CalculatePrice(
            apartment,
            period,
            nightlyPriceOverrides,
            options.Value.FeeRatesFor(apartment.Address.Country));
    }

    /// <summary>
    /// Gets the host service fee in percent.
    /// </summary>
    public decimal HostServiceFeePercent => options.Value.HostServiceFeePercent;
}
