using Lodge.Domain.Experiences;
using Lodge.Domain.Messaging;
using Microsoft.EntityFrameworkCore;

namespace Lodge.Persistence.Repositories;

/// <summary>
/// Represents the conversation repository.
/// </summary>
internal sealed class ConversationRepository(LodgeDbContext context) : IConversationRepository
{
    public Task<Conversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Conversations.FirstOrDefaultAsync(conversation => conversation.Id == id, cancellationToken);

    public Task<Conversation?> GetAsync(Guid apartmentId, Guid guestId, CancellationToken cancellationToken = default) =>
        context.Conversations.FirstOrDefaultAsync(
            conversation => conversation.ApartmentId == apartmentId && conversation.GuestId == guestId,
            cancellationToken);

    public Task<Message?> GetMessageAsync(Guid messageId, CancellationToken cancellationToken = default) =>
        context.Messages.FirstOrDefaultAsync(message => message.Id == messageId, cancellationToken);

    public void Insert(Conversation conversation) => context.Conversations.Add(conversation);

    public void InsertMessage(Message message) => context.Messages.Add(message);
}

/// <summary>
/// Represents the experience repository.
/// </summary>
internal sealed class ExperienceRepository(LodgeDbContext context) : IExperienceRepository
{
    private static readonly ExperienceBookingStatus[] ActiveStatuses =
        [ExperienceBookingStatus.Reserved, ExperienceBookingStatus.Confirmed];

    public Task<Experience?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Experiences.FirstOrDefaultAsync(experience => experience.Id == id, cancellationToken);

    public Task<ExperienceSession?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default) =>
        context.ExperienceSessions.FirstOrDefaultAsync(session => session.Id == sessionId, cancellationToken);

    public async Task<ExperienceSession?> GetSessionForUpdateAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        ExperienceSession? session = await context.ExperienceSessions
            .FromSql($"SELECT * FROM experience_sessions WHERE id = {sessionId} FOR UPDATE")
            .FirstOrDefaultAsync(cancellationToken);

        if (session is not null)
        {
            // An instance tracked before the lock may be stale: re-read the locked row.
            await context.Entry(session).ReloadAsync(cancellationToken);
        }

        return session;
    }

    public Task<List<DateTime>> GetSessionStartsAsync(Guid experienceId, CancellationToken cancellationToken = default) =>
        context.ExperienceSessions
            .Where(session => session.ExperienceId == experienceId && session.CancelledOnUtc == null)
            .Select(session => session.StartsAtUtc)
            .ToListAsync(cancellationToken);

    public Task<ExperienceBooking?> GetBookingAsync(Guid bookingId, CancellationToken cancellationToken = default) =>
        context.ExperienceBookings.FirstOrDefaultAsync(booking => booking.Id == bookingId, cancellationToken);

    public Task<List<ExperienceBooking>> GetActiveBookingsOfSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default) =>
        context.ExperienceBookings
            .Where(booking => booking.SessionId == sessionId && ActiveStatuses.Contains(booking.Status))
            .ToListAsync(cancellationToken);

    public Task<bool> SessionHasBookingsAsync(Guid sessionId, CancellationToken cancellationToken = default) =>
        context.ExperienceBookings.AnyAsync(booking => booking.SessionId == sessionId, cancellationToken);

    public Task<bool> HasUpcomingBookingsAsync(Guid experienceId, DateTime utcNow, CancellationToken cancellationToken = default) =>
        context.ExperienceBookings.AnyAsync(
            booking => booking.ExperienceId == experienceId &&
                       ActiveStatuses.Contains(booking.Status) &&
                       booking.SessionStartsAtUtc > utcNow,
            cancellationToken);

    public Task<bool> HasReviewAsync(Guid experienceBookingId, CancellationToken cancellationToken = default) =>
        context.ExperienceReviews.AnyAsync(review => review.ExperienceBookingId == experienceBookingId, cancellationToken);

    public void Insert(Experience experience) => context.Experiences.Add(experience);

    public void InsertSession(ExperienceSession session) => context.ExperienceSessions.Add(session);

    public void RemoveSession(ExperienceSession session) => context.ExperienceSessions.Remove(session);

    public void InsertBooking(ExperienceBooking booking) => context.ExperienceBookings.Add(booking);

    public void InsertReview(ExperienceReview review) => context.ExperienceReviews.Add(review);
}
