namespace Lodge.Contracts.Hosting;

/// <summary>
/// A row of the host's listings table.
/// </summary>
public sealed class HostListingSummaryResponse
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string PropertyType { get; init; } = string.Empty;

    public string City { get; init; } = string.Empty;

    public string Country { get; init; } = string.Empty;

    public string ImageUrl { get; set; } = string.Empty;

    public decimal Price { get; init; }

    public string Currency { get; init; } = string.Empty;

    public bool IsListed { get; init; }

    public bool InstantBook { get; init; }

    public decimal? AverageRating { get; init; }

    public int ReviewCount { get; init; }

    /// <summary>
    /// Gets the number of reserved or confirmed stays that have not ended yet.
    /// </summary>
    public int UpcomingBookingCount { get; init; }

    /// <summary>
    /// Gets the number of booking requests waiting for the host's answer.
    /// </summary>
    public int PendingRequestCount { get; init; }

    public DateTime CreatedOnUtc { get; init; }

    public DateTime? ModifiedOnUtc { get; init; }
}
