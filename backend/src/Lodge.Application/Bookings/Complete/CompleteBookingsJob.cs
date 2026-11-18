using System.Linq.Expressions;
using Lodge.Application.Abstractions.Data;
using Lodge.Domain.Bookings;
using Lodge.Domain.Core.Primitives;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lodge.Application.Bookings.Complete;

/// <summary>
/// Recurring job that completes finished stays through <see cref="Booking.Complete"/>, so the domain events
/// (and with them the "booking completed" e-mails) are raised exactly as for a manual completion.
/// </summary>
internal sealed partial class CompleteBookingsJob(
    IDbContext context,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider,
    ILogger<CompleteBookingsJob> logger) : ICompleteBookingsJob
{
    public async Task<int> CompleteFinishedBookingsAsync(CancellationToken cancellationToken = default)
    {
        DateTime utcNow = dateTimeProvider.UtcNow;

        List<Booking> bookings = await context.Set<Booking>()
            .Where(IsDueForCompletion(DateOnly.FromDateTime(utcNow)))
            .ToListAsync(cancellationToken);

        int completed = 0;

        foreach (Booking booking in bookings)
        {
            Result result = booking.Complete(utcNow);

            if (result.IsSuccess)
            {
                completed++;
            }
            else
            {
                LogCompletionFailed(logger, booking.Id, result.Error.Code);
            }
        }

        if (completed > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        LogCompleted(logger, completed);

        return completed;
    }

    /// <summary>
    /// Selects the confirmed bookings whose end (check-out) date is before <paramref name="today"/>.
    /// </summary>
    internal static Expression<Func<Booking, bool>> IsDueForCompletion(DateOnly today) =>
        booking => booking.Status == BookingStatus.Confirmed && booking.Duration.End < today;

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not complete booking {BookingId}: {ErrorCode}")]
    private static partial void LogCompletionFailed(ILogger logger, Guid bookingId, string errorCode);

    [LoggerMessage(Level = LogLevel.Information, Message = "Completed {Count} finished bookings")]
    private static partial void LogCompleted(ILogger logger, int count);
}
