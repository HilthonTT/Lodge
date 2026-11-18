namespace Lodge.Application.Notifications;

/// <summary>
/// The relative frontend paths notifications link to.
/// </summary>
public static class NotificationLinks
{
    public static string GuestTrip(Guid bookingId) => $"/bookings?bookingId={bookingId}";

    public static string HostReservation(Guid bookingId) => $"/hosting/reservations?bookingId={bookingId}";

    public static string Conversation(Guid conversationId) => $"/messages/{conversationId}";

    /// <summary>
    /// The reviews section of the listing page (<c>/stays/{id}</c> is the checkout page and needs dates).
    /// </summary>
    public static string ListingReviews(Guid apartmentId) => $"/apartments/{apartmentId}#reviews";

    public static string Profile(Guid userId) => $"/users/{userId}";

    public static string HostReviews => "/hosting/reviews";

    public static string Payouts => "/hosting/payouts";

    public static string ExperienceBooking(Guid experienceBookingId) => $"/experience-bookings/{experienceBookingId}";

    public static string HostExperienceBookings(Guid experienceId) => $"/hosting/experiences/{experienceId}/bookings";
}
