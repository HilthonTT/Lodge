using System.Data;
using Dapper;

namespace Lodge.Application.Notifications;

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
