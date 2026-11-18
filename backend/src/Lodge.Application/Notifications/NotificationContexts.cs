using System.Data;
using Dapper;

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

/// <summary>
/// The people, listing and dates of a booking, for notifications and e-mails.
/// </summary>
internal sealed class BookingContext
{
    public Guid BookingId { get; init; }

    public Guid ApartmentId { get; init; }

    public string ListingName { get; init; } = string.Empty;

    public Guid HostId { get; init; }

    public string HostFirstName { get; init; } = string.Empty;

    public string HostName { get; init; } = string.Empty;

    public string HostEmail { get; init; } = string.Empty;

    public Guid GuestId { get; init; }

    public string GuestFirstName { get; init; } = string.Empty;

    public string GuestName { get; init; } = string.Empty;

    public string GuestEmail { get; init; } = string.Empty;

    public DateTime CheckInValue { get; init; }

    public DateTime CheckOutValue { get; init; }

    public DateOnly CheckIn => DateOnly.FromDateTime(CheckInValue);

    public DateOnly CheckOut => DateOnly.FromDateTime(CheckOutValue);

    public int GuestCount { get; init; }

    public int Status { get; init; }

    public bool CancelledByHost { get; init; }

    public decimal? RefundAmount { get; init; }

    public string Currency { get; init; } = string.Empty;

    public string? CheckInInstructions { get; init; }

    public bool HasReview { get; init; }

    public bool HasGuestReview { get; init; }

    public const string Select =
        """
        SELECT
            b.id AS BookingId,
            a.id AS ApartmentId,
            a.name AS ListingName,
            h.id AS HostId,
            h.first_name AS HostFirstName,
            h.first_name || ' ' || h.last_name AS HostName,
            h.email AS HostEmail,
            g.id AS GuestId,
            g.first_name AS GuestFirstName,
            g.first_name || ' ' || g.last_name AS GuestName,
            g.email AS GuestEmail,
            b.duration_start::timestamp AS CheckInValue,
            b.duration_end::timestamp AS CheckOutValue,
            b.guest_count AS GuestCount,
            b.status AS Status,
            b.cancelled_by_host AS CancelledByHost,
            b.refund_amount AS RefundAmount,
            b.total_price_currency AS Currency,
            a.check_in_instructions AS CheckInInstructions,
            EXISTS (SELECT 1 FROM reviews AS r WHERE r.booking_id = b.id) AS HasReview,
            EXISTS (SELECT 1 FROM guest_reviews AS gr WHERE gr.booking_id = b.id) AS HasGuestReview
        FROM bookings AS b
        INNER JOIN apartments AS a ON a.id = b.apartment_id
        INNER JOIN users AS h ON h.id = a.owner_id
        INNER JOIN users AS g ON g.id = b.user_id
        """;

    public static Task<BookingContext?> GetAsync(IDbConnection connection, Guid bookingId) =>
        connection.QueryFirstOrDefaultAsync<BookingContext>(
            $"{Select} WHERE b.id = @BookingId",
            new { BookingId = bookingId });

    public object Data(object? extra = null)
    {
        var data = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["bookingId"] = BookingId,
            ["apartmentId"] = ApartmentId,
            ["listingName"] = ListingName,
            ["checkIn"] = CheckIn,
            ["checkOut"] = CheckOut,
            ["guestCount"] = GuestCount,
            ["guestFirstName"] = GuestFirstName,
            ["hostFirstName"] = HostFirstName,
        };

        if (extra is not null)
        {
            foreach (var property in extra.GetType().GetProperties())
            {
                data[char.ToLowerInvariant(property.Name[0]) + property.Name[1..]] = property.GetValue(extra);
            }
        }

        return data;
    }
}

/// <summary>
/// The people, experience and session of an experience booking, for notifications.
/// </summary>
internal sealed class ExperienceBookingContext
{
    public Guid ExperienceBookingId { get; init; }

    public Guid ExperienceId { get; init; }

    public string ExperienceTitle { get; init; } = string.Empty;

    public string MeetingPoint { get; init; } = string.Empty;

    public Guid HostId { get; init; }

    public Guid GuestId { get; init; }

    public string GuestFirstName { get; init; } = string.Empty;

    public string HostFirstName { get; init; } = string.Empty;

    public DateTime StartsAtUtc { get; init; }

    public int GuestCount { get; init; }

    public decimal? RefundAmount { get; init; }

    public string Currency { get; init; } = string.Empty;

    public static Task<ExperienceBookingContext?> GetAsync(IDbConnection connection, Guid experienceBookingId) =>
        connection.QueryFirstOrDefaultAsync<ExperienceBookingContext>(
            """
            SELECT
                eb.id AS ExperienceBookingId,
                e.id AS ExperienceId,
                e.title AS ExperienceTitle,
                e.meeting_point AS MeetingPoint,
                e.host_id AS HostId,
                eb.guest_id AS GuestId,
                g.first_name AS GuestFirstName,
                h.first_name AS HostFirstName,
                eb.session_starts_at_utc AS StartsAtUtc,
                eb.guest_count AS GuestCount,
                eb.refund_amount AS RefundAmount,
                eb.currency AS Currency
            FROM experience_bookings AS eb
            INNER JOIN experiences AS e ON e.id = eb.experience_id
            INNER JOIN users AS g ON g.id = eb.guest_id
            INNER JOIN users AS h ON h.id = e.host_id
            WHERE eb.id = @Id
            """,
            new { Id = experienceBookingId });

    public object Data(string role) => new
    {
        ExperienceBookingId,
        ExperienceId,
        ExperienceTitle,
        StartsAtUtc,
        GuestCount,
        MeetingPoint,
        GuestFirstName,
        HostFirstName,
        Role = role,
    };
}
