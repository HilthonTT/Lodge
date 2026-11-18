using System.Data;
using Dapper;

namespace Lodge.Application.Notifications;

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
