namespace Lodge.Contracts.Bookings;

/// <summary>
/// Represents the booking response.
/// </summary>
/// <param name="Id">The identifier.</param>
/// <param name="ApartmentId">The apartment identfier.</param>
/// <param name="UserId">The user identifier.</param>
/// <param name="Status">The status.</param>
/// <param name="PriceAmount">The price amount.</param>
/// <param name="PriceCurrency">The price currency.</param>
/// <param name="CleaningFeeAmount">The cleaning fee amount.</param>
/// <param name="CleaningFeeCurrency">The cleaning fee currency.</param>
/// <param name="AmenitiesUpChargeAmount">The amenities up charge amount.</param>
/// <param name="AmenitiesUpChargeCurrency">The amenities up charge currency.</param>
/// <param name="TotalPriceAmount">The total price amount.</param>
/// <param name="TotalPriceCurrency">The total price currency.</param>
/// <param name="DurationStart">The duration start.</param>
/// <param name="DurationEnd">The duration end.</param>
/// <param name="CreatedOnUtc">The created on utc.</param>
public sealed record BookingResponse(
    Guid Id,
    Guid ApartmentId,
    Guid UserId,
    int Status,
    decimal PriceAmount,
    string PriceCurrency,
    decimal CleaningFeeAmount,
    string CleaningFeeCurrency,
    decimal AmenitiesUpChargeAmount,
    string AmenitiesUpChargeCurrency,
    decimal TotalPriceAmount,
    string TotalPriceCurrency,
    DateOnly DurationStart,
    DateOnly DurationEnd,
    DateTime CreatedOnUtc);
