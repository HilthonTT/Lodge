using Lodge.Application.Abstractions.Messaging;

namespace Lodge.Application.Hosting.Bookings.CreateGuestReview;

/// <summary>
/// The host reviews the guest of a completed stay (<c>POST /hosting/bookings/{id}/guest-review</c>), within the review
/// window. When the guest has reviewed the listing too, both reviews are published.
/// </summary>
public sealed record CreateGuestReviewCommand(Guid BookingId, int Rating, string Comment) : ICommand<Guid>;
