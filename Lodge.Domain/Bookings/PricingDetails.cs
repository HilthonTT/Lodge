using Lodge.Domain.Shared;

namespace Lodge.Domain.Bookings;

/// <summary>
/// Represents the pricing details value object.
/// </summary>
public record PricingDetails(
    Money PriceForPeriod,
    Money CleaningFee,
    Money AmenitiesUpCharge,
    Money TotalPrice);
