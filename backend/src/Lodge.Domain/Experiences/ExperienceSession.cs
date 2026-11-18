using Lodge.Domain.Core.Primitives;

namespace Lodge.Domain.Experiences;

/// <summary>
/// A date and time at which an <see cref="Experience"/> takes place, with a number of spots.
/// </summary>
/// <remarks>
/// Spots are held as soon as a guest reserves (<see cref="BookedGuests"/>) and released when the booking is cancelled
/// or its payment hold expires. Callers lock the session row while changing <see cref="BookedGuests"/>, so concurrent
/// bookings can never overbook it.
/// </remarks>
public sealed class ExperienceSession : Entity
{
    private ExperienceSession(Guid id, Guid experienceId, DateTime startsAtUtc, int capacity, DateTime utcNow)
        : base(id)
    {
        ExperienceId = experienceId;
        StartsAtUtc = startsAtUtc;
        Capacity = capacity;
        CreatedOnUtc = utcNow;
    }

    /// <remarks>
    /// Required by EF Core.
    /// </remarks>
    private ExperienceSession()
    {
    }

    public Guid ExperienceId { get; private set; }

    public DateTime StartsAtUtc { get; private set; }

    public int Capacity { get; private set; }

    /// <summary>
    /// Gets the number of guests of the reserved and confirmed bookings.
    /// </summary>
    public int BookedGuests { get; private set; }

    public DateTime? CancelledOnUtc { get; private set; }

    public DateTime CreatedOnUtc { get; private set; }

    public bool IsCancelled => CancelledOnUtc is not null;

    public int SpotsLeft => Math.Max(0, Capacity - BookedGuests);

    public static Result<ExperienceSession> Create(
        Experience experience,
        DateTime startsAtUtc,
        int capacity,
        DateTime utcNow)
    {
        ArgumentNullException.ThrowIfNull(experience);

        if (capacity < 1 || capacity > experience.MaxGuests)
        {
            return Result.Failure<ExperienceSession>(ExperienceSessionErrors.InvalidCapacity);
        }

        DateTime startsAt = DateTime.SpecifyKind(startsAtUtc, DateTimeKind.Utc);

        if (startsAt <= utcNow)
        {
            return Result.Failure<ExperienceSession>(ExperienceSessionErrors.StartsInPast);
        }

        // Sessions start on whole minutes.
        startsAt = new DateTime(startsAt.Ticks - (startsAt.Ticks % TimeSpan.TicksPerMinute), DateTimeKind.Utc);

        return new ExperienceSession(Guid.NewGuid(), experience.Id, startsAt, capacity, utcNow);
    }

    /// <summary>
    /// Holds spots for a new booking.
    /// </summary>
    public Result Hold(int guestCount, DateTime utcNow)
    {
        if (IsCancelled)
        {
            return Result.Failure(ExperienceSessionErrors.Cancelled);
        }

        if (StartsAtUtc <= utcNow)
        {
            return Result.Failure(ExperienceSessionErrors.AlreadyStarted);
        }

        if (guestCount > SpotsLeft)
        {
            return Result.Failure(ExperienceSessionErrors.NotEnoughSpots);
        }

        BookedGuests += guestCount;

        return Result.Success();
    }

    /// <summary>
    /// Releases the spots of a cancelled booking.
    /// </summary>
    public void Release(int guestCount) => BookedGuests = Math.Max(0, BookedGuests - guestCount);

    public void Cancel(DateTime utcNow) => CancelledOnUtc ??= utcNow;

    /// <summary>
    /// Gets when the session ends.
    /// </summary>
    public DateTime EndsAtUtc(int durationMinutes) => StartsAtUtc.AddMinutes(durationMinutes);
}
