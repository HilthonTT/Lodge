namespace Lodge.Contracts.Bookings;

/// <summary>
/// Represents the booking response.
/// </summary>
public sealed class BookingResponse
{
    /// <summary>
    /// Gets or sets the identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the apartment identifier.
    /// </summary>
    public Guid ApartmentId { get; set; }

    /// <summary>
    /// Gets or sets the user identifier.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the status.
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// Gets or sets the price amount.
    /// </summary>
    public decimal PriceAmount { get; set; }

    /// <summary>
    /// Gets or sets the price currency.
    /// </summary>
    public string PriceCurrency { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the cleaning fee amount.
    /// </summary>
    public decimal CleaningFeeAmount { get; set; }

    /// <summary>
    /// Gets or sets the cleaning fee currency.
    /// </summary>
    public string CleaningFeeCurrency { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the amenities up charge amount.
    /// </summary>
    public decimal AmenitiesUpChargeAmount { get; set; }

    /// <summary>
    /// Gets or sets the amenities up charge currency.
    /// </summary>
    public string AmenitiesUpChargeCurrency { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the total price amount.
    /// </summary>
    public decimal TotalPriceAmount { get; set; }

    /// <summary>
    /// Gets or sets the total price currency.
    /// </summary>
    public string TotalPriceCurrency { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the apartment name.
    /// </summary>
    public string ApartmentName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the apartment image url.
    /// </summary>
    public string ApartmentImageUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the duration start.
    /// </summary>
    public DateTime DurationStart { get; set; }

    /// <summary>
    /// Gets or sets the duration end.
    /// </summary>
    public DateTime DurationEnd { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the booking was created in UTC.
    /// </summary>
    public DateTime CreatedOnUtc { get; set; }
}
