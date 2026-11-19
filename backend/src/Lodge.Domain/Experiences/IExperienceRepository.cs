namespace Lodge.Domain.Experiences;

/// <summary>
/// Represents the repository of experiences, their sessions, bookings and reviews.
/// </summary>
public interface IExperienceRepository
{
    Task<Experience?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ExperienceSession?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the session and locks its row (<c>SELECT … FOR UPDATE</c>) until the current transaction ends, so that
    /// concurrent bookings of the same session are serialised.
    /// </summary>
    Task<ExperienceSession?> GetSessionForUpdateAsync(Guid sessionId, CancellationToken cancellationToken = default);

    Task<List<DateTime>> GetSessionStartsAsync(Guid experienceId, CancellationToken cancellationToken = default);

    Task<ExperienceBooking?> GetBookingAsync(Guid bookingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Re-reads a tracked booking from the database. Call it after <see cref="GetSessionForUpdateAsync"/>: every
    /// status change of an active booking happens under its session's lock, so the reloaded state is current.
    /// </summary>
    Task ReloadBookingAsync(ExperienceBooking booking, CancellationToken cancellationToken = default);

    Task<List<ExperienceBooking>> GetActiveBookingsOfSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a value indicating whether the session has bookings of any status (sessions with bookings are cancelled,
    /// never removed).
    /// </summary>
    Task<bool> SessionHasBookingsAsync(Guid sessionId, CancellationToken cancellationToken = default);

    Task<bool> HasUpcomingBookingsAsync(Guid experienceId, DateTime utcNow, CancellationToken cancellationToken = default);

    Task<bool> HasReviewAsync(Guid experienceBookingId, CancellationToken cancellationToken = default);

    void Insert(Experience experience);

    void InsertSession(ExperienceSession session);

    void RemoveSession(ExperienceSession session);

    void InsertBooking(ExperienceBooking booking);

    void InsertReview(ExperienceReview review);
}
