namespace Lodge.Application.Bookings.Expire;

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
