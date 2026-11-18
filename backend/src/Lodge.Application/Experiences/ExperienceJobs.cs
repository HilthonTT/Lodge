using Lodge.Application.Abstractions.Data;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Experiences;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lodge.Application.Experiences;

internal sealed partial class ExperienceJobs(
    IDbContext context,
    IExperienceRepository experienceRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider,
    IOptions<ExperienceJobOptions> options,
    ILogger<ExperienceJobs> logger) : IExperienceJobs
{
    public async Task<int> CompletePastSessionsAsync(CancellationToken cancellationToken = default)
    {
        DateTime utcNow = dateTimeProvider.UtcNow;

        var candidates = await context.Set<ExperienceBooking>()
            .Where(booking => booking.Status == ExperienceBookingStatus.Confirmed && booking.SessionStartsAtUtc < utcNow)
            .Join(
                context.Set<Experience>().IgnoreQueryFilters(),
                booking => booking.ExperienceId,
                experience => experience.Id,
                (booking, experience) => new { Booking = booking, experience.DurationMinutes })
            .ToListAsync(cancellationToken);

        int completed = candidates.Count(candidate => candidate.Booking.Complete(utcNow, candidate.DurationMinutes).IsSuccess);

        if (completed > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        LogCompleted(logger, completed);

        return completed;
    }

    public async Task<int> ExpireUnpaidHoldsAsync(CancellationToken cancellationToken = default)
    {
        DateTime utcNow = dateTimeProvider.UtcNow;
        DateTime cutoff = utcNow.AddMinutes(-options.Value.PaymentHoldMinutes);

        List<ExperienceBooking> holds = await context.Set<ExperienceBooking>()
            .Where(booking => booking.Status == ExperienceBookingStatus.Reserved &&
                              (booking.CreatedOnUtc < cutoff || booking.SessionStartsAtUtc <= utcNow))
            .ToListAsync(cancellationToken);

        int released = 0;

        foreach (ExperienceBooking booking in holds)
        {
            // Locks the session row like a reservation, so the release cannot race with a new booking.
            await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

            ExperienceSession? session = await experienceRepository.GetSessionForUpdateAsync(booking.SessionId, cancellationToken);

            if (session is not null && booking.ExpireHold(session, utcNow))
            {
                released++;
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        LogReleased(logger, released);

        return released;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Completed {Count} experience bookings")]
    private static partial void LogCompleted(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Released {Count} unpaid experience holds")]
    private static partial void LogReleased(ILogger logger, int count);
}
