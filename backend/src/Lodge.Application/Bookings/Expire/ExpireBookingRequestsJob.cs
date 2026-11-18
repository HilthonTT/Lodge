using System.ComponentModel.DataAnnotations;
using Lodge.Application.Abstractions.Data;
using Lodge.Domain.Bookings;
using Lodge.Domain.Core.Primitives;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lodge.Application.Bookings.Expire;

/// <summary>
/// Settings of the job that expires unanswered booking requests (section <c>BackgroundJobs:ExpireBookingRequests</c>).
/// </summary>
public sealed class ExpireBookingRequestsOptions
{
    public const string SectionName = "BackgroundJobs:ExpireBookingRequests";

    /// <summary>
    /// Hangfire cron expression (UTC). Defaults to every 15 minutes.
    /// </summary>
    [Required]
    public string Schedule { get; set; } = "*/15 * * * *";

    /// <summary>
    /// How long a host has to answer a booking request. Defaults to 24 hours.
    /// </summary>
    [Range(1, 24 * 30)]
    public int ExpiryHours { get; set; } = 24;
}

/// <summary>
/// Expires unanswered booking requests.
/// </summary>
public interface IExpireBookingRequestsJob
{
    /// <summary>
    /// Expires the requests older than <see cref="ExpireBookingRequestsOptions.ExpiryHours"/> and the requests whose
    /// check-in date has passed.
    /// </summary>
    /// <returns>The number of expired requests.</returns>
    Task<int> ExpireAsync(CancellationToken cancellationToken = default);
}

internal sealed partial class ExpireBookingRequestsJob(
    IDbContext context,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider,
    IOptions<ExpireBookingRequestsOptions> options,
    ILogger<ExpireBookingRequestsJob> logger) : IExpireBookingRequestsJob
{
    public async Task<int> ExpireAsync(CancellationToken cancellationToken = default)
    {
        DateTime utcNow = dateTimeProvider.UtcNow;
        DateTime cutoff = utcNow.AddHours(-options.Value.ExpiryHours);
        DateOnly today = DateOnly.FromDateTime(utcNow);

        List<Booking> requests = await context.Set<Booking>()
            .Where(booking => booking.Status == BookingStatus.PendingApproval &&
                              (booking.CreatedOnUtc < cutoff || booking.Duration.Start < today))
            .ToListAsync(cancellationToken);

        int expired = requests.Count(booking => booking.Expire(utcNow).IsSuccess);

        if (expired > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        LogExpired(logger, expired);

        return expired;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Expired {Count} unanswered booking requests")]
    private static partial void LogExpired(ILogger logger, int count);
}
