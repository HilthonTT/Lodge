using Lodge.Application.Abstractions.Data;
using Lodge.Domain.Bookings;
using Lodge.Domain.Core.Primitives;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lodge.Application.Bookings.Expire;

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
