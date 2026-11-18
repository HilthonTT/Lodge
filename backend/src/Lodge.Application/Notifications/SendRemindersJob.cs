using System.Data;
using Dapper;
using Lodge.Application.Abstractions.App;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Notifications;
using Lodge.Contracts.Emails;
using Lodge.Domain.Bookings;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Experiences;
using Lodge.Domain.Notifications;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lodge.Application.Notifications;

internal sealed partial class SendRemindersJob(
    IDbConnectionFactory connectionFactory,
    INotificationService notificationService,
    INotificationPreferencesProvider preferencesProvider,
    IEmailNotificationService emailNotificationService,
    IDateTimeProvider dateTimeProvider,
    IOptions<AppOptions> appOptions,
    ILogger<SendRemindersJob> logger) : ISendRemindersJob
{
    public async Task<int> SendAsync(CancellationToken cancellationToken = default)
    {
        DateOnly today = DateOnly.FromDateTime(dateTimeProvider.UtcNow);

        List<BookingContext> trips;
        List<BookingContext> arrivals;
        List<BookingContext> checkedOut;
        List<ExperienceBookingContext> experiences;

        using (IDbConnection connection = await connectionFactory.GetOpenConnectionAsync(cancellationToken))
        {
            trips = await GetBookingsAsync(connection, "b.status = @Confirmed AND b.duration_start = @Date", today.AddDays(2));
            arrivals = await GetBookingsAsync(connection, "b.status = @Confirmed AND b.duration_start = @Date", today.AddDays(1));
            checkedOut = await GetBookingsAsync(connection, "b.status = @Completed AND b.duration_end = @Date", today.AddDays(-1));
            experiences = await GetExperienceBookingsAsync(connection, today.AddDays(1));
        }

        int created = 0;

        foreach (BookingContext trip in trips)
        {
            bool isNew = await notificationService.NotifyAsync(
                new NotificationRequest(
                    trip.GuestId,
                    NotificationTypes.TripReminder,
                    trip.Data(),
                    NotificationLinks.GuestTrip(trip.BookingId),
                    $"booking:{trip.BookingId}:trip-reminder"),
                cancellationToken);

            if (isNew)
            {
                created++;

                if (await preferencesProvider.AllowsEmailAsync(trip.GuestId, NotificationCategory.Reminders, cancellationToken))
                {
                    await emailNotificationService.SendTripReminderEmailAsync(
                        new TripReminderEmail(
                            trip.GuestEmail,
                            trip.GuestName,
                            trip.ListingName,
                            trip.CheckIn,
                            trip.CheckInInstructions,
                            appOptions.Value.FrontendUrl(NotificationLinks.GuestTrip(trip.BookingId))),
                        cancellationToken);
                }
            }
        }

        foreach (BookingContext arrival in arrivals)
        {
            created += await notificationService.NotifyAsync(
                new NotificationRequest(
                    arrival.HostId,
                    NotificationTypes.ArrivalReminder,
                    arrival.Data(),
                    NotificationLinks.HostReservation(arrival.BookingId),
                    $"booking:{arrival.BookingId}:arrival-reminder"),
                cancellationToken) ? 1 : 0;
        }

        foreach (BookingContext stay in checkedOut)
        {
            DateOnly deadline = stay.CheckOut.AddDays(Booking.ReviewWindowDays);

            if (!stay.HasReview)
            {
                created += await RemindToReviewAsync(stay, stay.GuestId, stay.GuestEmail, stay.GuestName, forHost: false, deadline, cancellationToken);
            }

            if (!stay.HasGuestReview)
            {
                created += await RemindToReviewAsync(stay, stay.HostId, stay.HostEmail, stay.HostName, forHost: true, deadline, cancellationToken);
            }
        }

        foreach (ExperienceBookingContext experience in experiences)
        {
            created += await notificationService.NotifyAsync(
                new NotificationRequest(
                    experience.GuestId,
                    NotificationTypes.ExperienceReminder,
                    experience.Data("Guest"),
                    NotificationLinks.ExperienceBooking(experience.ExperienceBookingId),
                    $"experience-booking:{experience.ExperienceBookingId}:reminder"),
                cancellationToken) ? 1 : 0;
        }

        LogSent(logger, created);

        return created;
    }

    private async Task<int> RemindToReviewAsync(
        BookingContext stay,
        Guid userId,
        string email,
        string name,
        bool forHost,
        DateOnly deadline,
        CancellationToken cancellationToken)
    {
        string link = forHost ? NotificationLinks.HostReservation(stay.BookingId) : NotificationLinks.GuestTrip(stay.BookingId);

        bool isNew = await notificationService.NotifyAsync(
            new NotificationRequest(
                userId,
                NotificationTypes.ReviewReminder,
                stay.Data(new { Role = forHost ? "Host" : "Guest", ReviewDeadline = deadline }),
                link,
                $"booking:{stay.BookingId}:review-reminder"),
            cancellationToken);

        if (!isNew)
        {
            return 0;
        }

        if (await preferencesProvider.AllowsEmailAsync(userId, NotificationCategory.Reminders, cancellationToken))
        {
            await emailNotificationService.SendReviewReminderEmailAsync(
                new ReviewReminderEmail(email, name, stay.ListingName, forHost, deadline, appOptions.Value.FrontendUrl(link)),
                cancellationToken);
        }

        return 1;
    }

    private static async Task<List<BookingContext>> GetBookingsAsync(IDbConnection connection, string where, DateOnly date) =>
        [
            .. await connection.QueryAsync<BookingContext>(
                $"{BookingContext.Select} WHERE {where}",
                new
                {
                    Confirmed = (int)BookingStatus.Confirmed,
                    Completed = (int)BookingStatus.Completed,
                    Date = date.ToDateTime(TimeOnly.MinValue),
                }),
        ];

    private static async Task<List<ExperienceBookingContext>> GetExperienceBookingsAsync(IDbConnection connection, DateOnly date)
    {
        IEnumerable<Guid> ids = await connection.QueryAsync<Guid>(
            """
            SELECT id FROM experience_bookings
            WHERE status = @Confirmed AND session_starts_at_utc >= @From AND session_starts_at_utc < @To
            """,
            new
            {
                Confirmed = nameof(ExperienceBookingStatus.Confirmed),
                From = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                To = date.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            });

        List<ExperienceBookingContext> bookings = [];

        foreach (Guid id in ids)
        {
            if (await ExperienceBookingContext.GetAsync(connection, id) is { } booking)
            {
                bookings.Add(booking);
            }
        }

        return bookings;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Created {Count} reminders")]
    private static partial void LogSent(ILogger logger, int count);
}
