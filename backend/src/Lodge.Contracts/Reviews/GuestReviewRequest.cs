namespace Lodge.Contracts.Reviews;

/// <summary>
/// The host reviews the guest of a completed stay (<c>POST /hosting/bookings/{bookingId}/guest-review</c>).
/// </summary>
/// <param name="Rating">The rating (1 to 5).</param>
/// <param name="Comment">The comment (at most 500 characters).</param>
public sealed record GuestReviewRequest(int Rating, string Comment);
