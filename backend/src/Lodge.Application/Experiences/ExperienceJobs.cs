using System.ComponentModel.DataAnnotations;
using Lodge.Application.Abstractions.Data;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Experiences;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lodge.Application.Experiences;

/// <summary>
/// The recurring experience jobs.
/// </summary>
public interface IExperienceJobs
{
    /// <summary>
    /// Completes the confirmed bookings whose session has ended (daily).
    /// </summary>
    /// <returns>The number of completed bookings.</returns>
    Task<int> CompletePastSessionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases the spots of unpaid bookings older than <see cref="ExperienceJobOptions.PaymentHoldMinutes"/>, and of
    /// unpaid bookings whose session has started.
    /// </summary>
    /// <returns>The number of released bookings.</returns>
    Task<int> ExpireUnpaidHoldsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Settings of the experience jobs (section <c>BackgroundJobs:Experiences</c>).
/// </summary>
public sealed class ExperienceJobOptions
{
    public const string SectionName = "BackgroundJobs:Experiences";

    /// <summary>
    /// Hangfire cron expression (UTC) of the completion job. Defaults to daily at 03:15.
    /// </summary>
    [Required]
    public string CompleteSchedule { get; set; } = "15 3 * * *";

    /// <summary>
    /// Hangfire cron expression (UTC) of the job that releases unpaid holds. Defaults to every 15 minutes.
    /// </summary>
    [Required]
    public string ExpireHoldsSchedule { get; set; } = "*/15 * * * *";

    /// <summary>
    /// How long spots stay held for an unpaid booking. Defaults to 60 minutes.
    /// </summary>
    [Range(5, 24 * 60)]
    public int PaymentHoldMinutes { get; set; } = 60;
}

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
