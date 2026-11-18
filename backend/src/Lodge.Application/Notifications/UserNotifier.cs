using System.Data;
using Dapper;
using Lodge.Application.Abstractions.App;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Notifications;
using Lodge.Contracts.Emails;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Messaging;
using Lodge.Domain.Notifications;
using Microsoft.Extensions.Options;

namespace Lodge.Application.Notifications;

/// <summary>
/// Turns the platform's events into in-app notifications (and the e-mails that have no handler of their own). Called by
/// the integration event handlers of the background tasks; every method is idempotent per source event.
/// </summary>
public interface IUserNotifier
{
    Task BookingRequestedAsync(Guid bookingId, CancellationToken cancellationToken = default);

    Task BookingAcceptedAsync(Guid bookingId, CancellationToken cancellationToken = default);

    Task BookingDeclinedAsync(Guid bookingId, CancellationToken cancellationToken = default);

    Task BookingExpiredAsync(Guid bookingId, CancellationToken cancellationToken = default);

    Task BookingConfirmedAsync(Guid bookingId, CancellationToken cancellationToken = default);

    Task BookingCancelledAsync(Guid bookingId, CancellationToken cancellationToken = default);

    Task BookingDatesChangedAsync(
        Guid bookingId,
        DateOnly previousStart,
        DateOnly previousEnd,
        CancellationToken cancellationToken = default);

    Task BookingRefundSucceededAsync(Guid bookingId, CancellationToken cancellationToken = default);

    Task ReviewPublishedAsync(Guid reviewId, CancellationToken cancellationToken = default);

    Task GuestReviewPublishedAsync(Guid guestReviewId, CancellationToken cancellationToken = default);

    Task ReviewRepliedAsync(Guid reviewId, CancellationToken cancellationToken = default);

    Task PayoutPaidAsync(Guid payoutId, CancellationToken cancellationToken = default);

    Task ExperienceBookingConfirmedAsync(Guid experienceBookingId, CancellationToken cancellationToken = default);

    Task ExperienceRefundSucceededAsync(Guid experienceBookingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// E-mails the recipient of a new message (if they allow message e-mails, have not read it yet and were not
    /// e-mailed about this conversation in the last 15 minutes).
    /// </summary>
    Task MessageSentAsync(Guid messageId, CancellationToken cancellationToken = default);
}

internal sealed class UserNotifier(
    IDbConnectionFactory connectionFactory,
    INotificationService notificationService,
    INotificationPreferencesProvider preferencesProvider,
    IEmailNotificationService emailNotificationService,
    IConversationRepository conversationRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider,
    IOptions<AppOptions> appOptions) : IUserNotifier
{
    /// <summary>
    /// A participant gets at most one "new message" e-mail per conversation in this period.
    /// </summary>
    internal static readonly TimeSpan MessageEmailThrottle = TimeSpan.FromMinutes(15);

    public Task BookingRequestedAsync(Guid bookingId, CancellationToken cancellationToken = default) =>
        WithBookingAsync(bookingId, booking => [
            new NotificationRequest(
                booking.HostId,
                NotificationTypes.BookingRequested,
                booking.Data(),
                NotificationLinks.HostReservation(booking.BookingId),
                $"booking:{booking.BookingId}:requested"),
        ], cancellationToken);

    public Task BookingAcceptedAsync(Guid bookingId, CancellationToken cancellationToken = default) =>
        WithBookingAsync(bookingId, booking => [
            new NotificationRequest(
                booking.GuestId,
                NotificationTypes.BookingAccepted,
                booking.Data(),
                NotificationLinks.GuestTrip(booking.BookingId),
                $"booking:{booking.BookingId}:accepted"),
        ], cancellationToken);

    public Task BookingDeclinedAsync(Guid bookingId, CancellationToken cancellationToken = default) =>
        WithBookingAsync(bookingId, booking => [
            new NotificationRequest(
                booking.GuestId,
                NotificationTypes.BookingDeclined,
                booking.Data(),
                NotificationLinks.GuestTrip(booking.BookingId),
                $"booking:{booking.BookingId}:declined"),
        ], cancellationToken);

    public Task BookingExpiredAsync(Guid bookingId, CancellationToken cancellationToken = default) =>
        WithBookingAsync(bookingId, booking => [
            new NotificationRequest(
                booking.GuestId,
                NotificationTypes.BookingExpired,
                booking.Data(),
                NotificationLinks.GuestTrip(booking.BookingId),
                $"booking:{booking.BookingId}:expired"),
        ], cancellationToken);

    public Task BookingConfirmedAsync(Guid bookingId, CancellationToken cancellationToken = default) =>
        WithBookingAsync(bookingId, booking => [
            new NotificationRequest(
                booking.GuestId,
                NotificationTypes.BookingConfirmed,
                booking.Data(new { Role = "Guest" }),
                NotificationLinks.GuestTrip(booking.BookingId),
                $"booking:{booking.BookingId}:confirmed"),
            new NotificationRequest(
                booking.HostId,
                NotificationTypes.BookingConfirmed,
                booking.Data(new { Role = "Host" }),
                NotificationLinks.HostReservation(booking.BookingId),
                $"booking:{booking.BookingId}:confirmed"),
        ], cancellationToken);

    public Task BookingCancelledAsync(Guid bookingId, CancellationToken cancellationToken = default) =>
        WithBookingAsync(bookingId, booking =>
        {
            object data = booking.Data(new
            {
                CancelledBy = booking.CancelledByHost ? "Host" : "Guest",
                booking.RefundAmount,
                booking.Currency,
            });

            // The other party is told: the guest when the host cancelled, otherwise the host.
            return booking.CancelledByHost
                ? [new NotificationRequest(
                    booking.GuestId,
                    NotificationTypes.BookingCancelled,
                    data,
                    NotificationLinks.GuestTrip(booking.BookingId),
                    $"booking:{booking.BookingId}:cancelled")]
                : [new NotificationRequest(
                    booking.HostId,
                    NotificationTypes.BookingCancelled,
                    data,
                    NotificationLinks.HostReservation(booking.BookingId),
                    $"booking:{booking.BookingId}:cancelled")];
        }, cancellationToken);

    public Task BookingDatesChangedAsync(
        Guid bookingId,
        DateOnly previousStart,
        DateOnly previousEnd,
        CancellationToken cancellationToken = default) =>
        WithBookingAsync(bookingId, booking => [
            new NotificationRequest(
                booking.HostId,
                NotificationTypes.BookingDatesChanged,
                booking.Data(new { PreviousCheckIn = previousStart, PreviousCheckOut = previousEnd }),
                NotificationLinks.HostReservation(booking.BookingId),
                $"booking:{booking.BookingId}:dates:{previousStart:yyyyMMdd}-{previousEnd:yyyyMMdd}:{booking.CheckIn:yyyyMMdd}-{booking.CheckOut:yyyyMMdd}"),
        ], cancellationToken);

    public Task BookingRefundSucceededAsync(Guid bookingId, CancellationToken cancellationToken = default) =>
        WithBookingAsync(bookingId, booking => [
            new NotificationRequest(
                booking.GuestId,
                NotificationTypes.RefundSucceeded,
                booking.Data(new { Amount = booking.RefundAmount, booking.Currency }),
                NotificationLinks.GuestTrip(booking.BookingId),
                $"booking:{booking.BookingId}:refund"),
        ], cancellationToken);

    public async Task ReviewPublishedAsync(Guid reviewId, CancellationToken cancellationToken = default)
    {
        using IDbConnection connection = await connectionFactory.GetOpenConnectionAsync(cancellationToken);

        ReviewRow? review = await connection.QueryFirstOrDefaultAsync<ReviewRow>(
            """
            SELECT r.id AS ReviewId, r.rating AS Rating, r.booking_id AS BookingId, a.id AS ApartmentId,
                   a.name AS ListingName, a.owner_id AS RecipientId, g.first_name AS ReviewerFirstName
            FROM reviews AS r
            INNER JOIN apartments AS a ON a.id = r.apartment_id
            INNER JOIN users AS g ON g.id = r.user_id
            WHERE r.id = @ReviewId AND r.is_published
            """,
            new { ReviewId = reviewId });

        if (review is null)
        {
            return;
        }

        await notificationService.NotifyAsync(
            new NotificationRequest(
                review.RecipientId,
                NotificationTypes.ReviewReceived,
                review.Data("Listing"),
                NotificationLinks.HostReviews,
                $"review:{review.ReviewId}:published"),
            cancellationToken);
    }

    public async Task GuestReviewPublishedAsync(Guid guestReviewId, CancellationToken cancellationToken = default)
    {
        using IDbConnection connection = await connectionFactory.GetOpenConnectionAsync(cancellationToken);

        ReviewRow? review = await connection.QueryFirstOrDefaultAsync<ReviewRow>(
            """
            SELECT r.id AS ReviewId, r.rating AS Rating, r.booking_id AS BookingId, a.id AS ApartmentId,
                   a.name AS ListingName, r.guest_id AS RecipientId, h.first_name AS ReviewerFirstName
            FROM guest_reviews AS r
            INNER JOIN apartments AS a ON a.id = r.apartment_id
            INNER JOIN users AS h ON h.id = r.host_id
            WHERE r.id = @ReviewId AND r.is_published
            """,
            new { ReviewId = guestReviewId });

        if (review is null)
        {
            return;
        }

        await notificationService.NotifyAsync(
            new NotificationRequest(
                review.RecipientId,
                NotificationTypes.ReviewReceived,
                review.Data("Guest"),
                NotificationLinks.Profile(review.RecipientId),
                $"guest-review:{review.ReviewId}:published"),
            cancellationToken);
    }

    public async Task ReviewRepliedAsync(Guid reviewId, CancellationToken cancellationToken = default)
    {
        using IDbConnection connection = await connectionFactory.GetOpenConnectionAsync(cancellationToken);

        ReviewRow? review = await connection.QueryFirstOrDefaultAsync<ReviewRow>(
            """
            SELECT r.id AS ReviewId, r.rating AS Rating, r.booking_id AS BookingId, a.id AS ApartmentId,
                   a.name AS ListingName, r.user_id AS RecipientId, h.first_name AS ReviewerFirstName
            FROM reviews AS r
            INNER JOIN apartments AS a ON a.id = r.apartment_id
            INNER JOIN users AS h ON h.id = a.owner_id
            WHERE r.id = @ReviewId AND r.host_reply IS NOT NULL
            """,
            new { ReviewId = reviewId });

        if (review is null)
        {
            return;
        }

        await notificationService.NotifyAsync(
            new NotificationRequest(
                review.RecipientId,
                NotificationTypes.ReviewReplyReceived,
                new
                {
                    review.ReviewId,
                    review.BookingId,
                    review.ApartmentId,
                    review.ListingName,
                    HostFirstName = review.ReviewerFirstName,
                },
                NotificationLinks.ListingReviews(review.ApartmentId),
                $"review:{review.ReviewId}:reply"),
            cancellationToken);
    }

    public async Task PayoutPaidAsync(Guid payoutId, CancellationToken cancellationToken = default)
    {
        using IDbConnection connection = await connectionFactory.GetOpenConnectionAsync(cancellationToken);

        PayoutRow? payout = await connection.QueryFirstOrDefaultAsync<PayoutRow>(
            """
            SELECT p.id AS PayoutId, p.host_id AS HostId, p.amount_amount AS Amount, p.amount_currency AS Currency,
                   p.booking_id AS BookingId, p.experience_booking_id AS ExperienceBookingId,
                   COALESCE(a.name, e.title, '') AS ListingName
            FROM payouts AS p
            LEFT JOIN bookings AS b ON b.id = p.booking_id
            LEFT JOIN apartments AS a ON a.id = b.apartment_id
            LEFT JOIN experience_bookings AS eb ON eb.id = p.experience_booking_id
            LEFT JOIN experiences AS e ON e.id = eb.experience_id
            WHERE p.id = @PayoutId AND p.status = 'Paid'
            """,
            new { PayoutId = payoutId });

        if (payout is null)
        {
            return;
        }

        await notificationService.NotifyAsync(
            new NotificationRequest(
                payout.HostId,
                NotificationTypes.PayoutPaid,
                new
                {
                    payout.PayoutId,
                    payout.Amount,
                    payout.Currency,
                    payout.BookingId,
                    payout.ExperienceBookingId,
                    payout.ListingName,
                },
                NotificationLinks.Payouts,
                $"payout:{payout.PayoutId}:paid"),
            cancellationToken);
    }

    public async Task ExperienceBookingConfirmedAsync(Guid experienceBookingId, CancellationToken cancellationToken = default)
    {
        using IDbConnection connection = await connectionFactory.GetOpenConnectionAsync(cancellationToken);

        ExperienceBookingContext? booking = await ExperienceBookingContext.GetAsync(connection, experienceBookingId);

        if (booking is null)
        {
            return;
        }

        await notificationService.NotifyAsync(
            new NotificationRequest(
                booking.GuestId,
                NotificationTypes.ExperienceBooked,
                booking.Data("Guest"),
                NotificationLinks.ExperienceBooking(booking.ExperienceBookingId),
                $"experience-booking:{booking.ExperienceBookingId}:confirmed"),
            cancellationToken);

        await notificationService.NotifyAsync(
            new NotificationRequest(
                booking.HostId,
                NotificationTypes.ExperienceBooked,
                booking.Data("Host"),
                NotificationLinks.HostExperienceBookings(booking.ExperienceId),
                $"experience-booking:{booking.ExperienceBookingId}:confirmed"),
            cancellationToken);
    }

    public async Task ExperienceRefundSucceededAsync(Guid experienceBookingId, CancellationToken cancellationToken = default)
    {
        using IDbConnection connection = await connectionFactory.GetOpenConnectionAsync(cancellationToken);

        ExperienceBookingContext? booking = await ExperienceBookingContext.GetAsync(connection, experienceBookingId);

        if (booking is null)
        {
            return;
        }

        await notificationService.NotifyAsync(
            new NotificationRequest(
                booking.GuestId,
                NotificationTypes.RefundSucceeded,
                new
                {
                    booking.ExperienceBookingId,
                    booking.ExperienceId,
                    booking.ExperienceTitle,
                    Amount = booking.RefundAmount,
                    booking.Currency,
                },
                NotificationLinks.ExperienceBooking(booking.ExperienceBookingId),
                $"experience-booking:{booking.ExperienceBookingId}:refund"),
            cancellationToken);
    }

    public async Task MessageSentAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        Message? message = await conversationRepository.GetMessageAsync(messageId, cancellationToken);

        if (message?.SenderId is not Guid senderId)
        {
            return;
        }

        Conversation? conversation = await conversationRepository.GetByIdAsync(message.ConversationId, cancellationToken);

        if (conversation is null)
        {
            return;
        }

        Guid recipientId = conversation.OtherParticipant(senderId);

        if (conversation.LastReadOnUtc(recipientId) >= message.SentOnUtc ||
            !await preferencesProvider.AllowsEmailAsync(recipientId, NotificationCategory.Messages, cancellationToken))
        {
            return;
        }

        if (!conversation.TryRecordEmail(recipientId, dateTimeProvider.UtcNow, MessageEmailThrottle))
        {
            return;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        using IDbConnection connection = await connectionFactory.GetOpenConnectionAsync(cancellationToken);

        MessageEmailRow? row = await connection.QueryFirstOrDefaultAsync<MessageEmailRow>(
            """
            SELECT r.email AS RecipientEmail, r.first_name || ' ' || r.last_name AS RecipientName,
                   s.first_name AS SenderFirstName, a.name AS ListingName
            FROM users AS r, users AS s, apartments AS a
            WHERE r.id = @RecipientId AND s.id = @SenderId AND a.id = @ApartmentId
            """,
            new { RecipientId = recipientId, SenderId = senderId, conversation.ApartmentId });

        if (row is null)
        {
            return;
        }

        string preview = message.Body.Length > 200 ? $"{message.Body[..200]}…" : message.Body;

        await emailNotificationService.SendNewMessageEmailAsync(
            new NewMessageEmail(
                row.RecipientEmail,
                row.RecipientName,
                row.SenderFirstName,
                row.ListingName,
                preview,
                appOptions.Value.FrontendUrl(NotificationLinks.Conversation(conversation.Id))),
            cancellationToken);
    }

    private async Task WithBookingAsync(
        Guid bookingId,
        Func<BookingContext, NotificationRequest[]> notifications,
        CancellationToken cancellationToken)
    {
        BookingContext? booking;

        using (IDbConnection connection = await connectionFactory.GetOpenConnectionAsync(cancellationToken))
        {
            booking = await BookingContext.GetAsync(connection, bookingId);
        }

        if (booking is null)
        {
            return;
        }

        foreach (NotificationRequest notification in notifications(booking))
        {
            await notificationService.NotifyAsync(notification, cancellationToken);
        }
    }

    private sealed class ReviewRow
    {
        public Guid ReviewId { get; init; }

        public int Rating { get; init; }

        public Guid BookingId { get; init; }

        public Guid ApartmentId { get; init; }

        public string ListingName { get; init; } = string.Empty;

        public Guid RecipientId { get; init; }

        public string ReviewerFirstName { get; init; } = string.Empty;

        public object Data(string kind) => new
        {
            ReviewId,
            Kind = kind,
            Rating,
            BookingId,
            ApartmentId,
            ListingName,
            ReviewerFirstName,
        };
    }

    private sealed class PayoutRow
    {
        public Guid PayoutId { get; init; }

        public Guid HostId { get; init; }

        public decimal Amount { get; init; }

        public string Currency { get; init; } = string.Empty;

        public Guid? BookingId { get; init; }

        public Guid? ExperienceBookingId { get; init; }

        public string ListingName { get; init; } = string.Empty;
    }

    private sealed class MessageEmailRow
    {
        public string RecipientEmail { get; init; } = string.Empty;

        public string RecipientName { get; init; } = string.Empty;

        public string SenderFirstName { get; init; } = string.Empty;

        public string ListingName { get; init; } = string.Empty;
    }
}
