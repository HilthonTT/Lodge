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

/// <summary>
/// The category ratings of a review (each 1 to 5).
/// </summary>
public sealed record CategoryRatingsDto
{
    public int Cleanliness { get; init; }

    public int Accuracy { get; init; }

    public int CheckIn { get; init; }

    public int Communication { get; init; }

    public int Location { get; init; }

    public int Value { get; init; }
}

/// <summary>
/// The host's reply to a review.
/// </summary>
public sealed record HostReplyResponse(string Text, DateTime CreatedOnUtc, DateTime? ModifiedOnUtc);

/// <summary>
/// Creates or edits the host's reply to a published review (<c>PUT /hosting/reviews/{reviewId}/reply</c>).
/// </summary>
/// <param name="Text">The reply (1 to 1000 characters).</param>
public sealed record ReviewReplyRequest(string Text);

/// <summary>
/// The host reviews the guest of a completed stay (<c>POST /hosting/bookings/{bookingId}/guest-review</c>).
/// </summary>
/// <param name="Rating">The rating (1 to 5).</param>
/// <param name="Comment">The comment (at most 500 characters).</param>
public sealed record GuestReviewRequest(int Rating, string Comment);
