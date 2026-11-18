namespace Lodge.Contracts.Reviews;

/// <summary>
/// Represents a guest's review of a listing.
/// </summary>
public sealed class ReviewResponse
{
    public Guid Id { get; init; }

    public Guid ApartmentId { get; init; }

    /// <summary>
    /// Gets the listing name.
    /// </summary>
    public string ListingName { get; init; } = string.Empty;

    public Guid BookingId { get; init; }

    /// <summary>
    /// Gets the author (guest) identifier.
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// Gets the overall rating (1 to 5).
    /// </summary>
    public int Rating { get; init; }

    public CategoryRatingsDto Categories { get; init; } = new();

    public string Comment { get; init; } = string.Empty;

    public DateTime CreatedOnUtc { get; init; }

    public DateTime? ModifiedOnUtc { get; init; }

    /// <summary>
    /// Gets the author's first name and last initial, e.g. <c>"Jamie L."</c>.
    /// </summary>
    public string AuthorName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the absolute URL of the author's avatar, or null.
    /// </summary>
    public string? AuthorAvatarUrl { get; init; }

    /// <summary>
    /// Gets the host's public reply, or null.
    /// </summary>
    public HostReplyResponse? HostReply { get; init; }

    /// <summary>
    /// Gets a value indicating whether the review is published. Unpublished reviews are only returned to their
    /// author.
    /// </summary>
    public bool IsPublished { get; init; }
}
