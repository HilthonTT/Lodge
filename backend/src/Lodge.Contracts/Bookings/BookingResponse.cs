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

    /// <summary>
    /// Gets or sets the guest's full name.
    /// </summary>
    public string GuestName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the guest's e-mail address.
    /// </summary>
    public string GuestEmail { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether a review exists for this booking.
    /// </summary>
    public bool HasReview { get; set; }

    /// <summary>
    /// Gets or sets the weekly or monthly discount already deducted from the total (same currency as the price).
    /// </summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>
    /// Gets or sets the number of guests.
    /// </summary>
    public int GuestCount { get; set; }

    /// <summary>
    /// Gets or sets the guest's message to the host, or null.
    /// </summary>
    public string? MessageToHost { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the listing's host.
    /// </summary>
    public Guid ListingHostId { get; set; }

    /// <summary>
    /// Gets or sets the listing's check-in instructions: only set for the host, and for the guest once the booking
    /// is confirmed; null otherwise.
    /// </summary>
    public string? CheckInInstructions { get; set; }

    /// <summary>
    /// Gets or sets the guest service fee (included in the total).
    /// </summary>
    public decimal ServiceFeeAmount { get; set; }

    /// <summary>
    /// Gets or sets the occupancy tax (included in the total).
    /// </summary>
    public decimal TaxAmount { get; set; }

    /// <summary>
    /// Gets or sets the occupancy tax rate, in percent.
    /// </summary>
    public decimal TaxRatePercent { get; set; }

    /// <summary>
    /// Gets or sets the cancellation policy snapshotted at reservation: <c>Flexible</c>, <c>Moderate</c> or
    /// <c>Strict</c>.
    /// </summary>
    public string CancellationPolicy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the amount paid, or null when unpaid (or paid before payment tracking existed).
    /// </summary>
    public decimal? AmountPaid { get; set; }

    public DateTime? PaidOnUtc { get; set; }

    /// <summary>
    /// Gets or sets the refund of a cancelled paid booking, or null.
    /// </summary>
    public decimal? RefundAmount { get; set; }

    /// <summary>
    /// Gets or sets <c>None</c>, <c>Pending</c>, <c>Succeeded</c>, <c>Failed</c> or <c>Manual</c>.
    /// </summary>
    public string RefundStatus { get; set; } = "None";

    /// <summary>
    /// Gets or sets a value indicating whether the host cancelled the booking.
    /// </summary>
    public bool CancelledByHost { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the caller can review this stay now: the guest (listing review) or
    /// the host (guest review) of a completed stay, within the review window, without a review yet.
    /// </summary>
    public bool CanReview { get; set; }

    /// <summary>
    /// Gets or sets the last day on which the stay can be reviewed (check-out + 14 days) for completed stays, else
    /// null.
    /// </summary>
    public DateOnly? ReviewDeadline { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the host has reviewed the guest of this stay.
    /// </summary>
    public bool HasGuestReview { get; set; }

    /// <summary>
    /// Gets or sets when an unanswered request (status 6) expires: created + <c>ExpiryHours</c> (24 by default), or
    /// at the end of the check-in day (UTC) if that comes first; null for other statuses. The expiry job runs every
    /// 15 minutes, so the status changes shortly after.
    /// </summary>
    public DateTime? RequestExpiresOnUtc { get; set; }
}
