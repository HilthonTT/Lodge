namespace Lodge.Application.Notifications;

/// <summary>
/// Sends the daily reminders.
/// </summary>
public interface ISendRemindersJob
{
    /// <summary>
    /// Creates (once per booking) the trip reminders (guests checking in in two days, with an e-mail), the arrival
    /// reminders (hosts whose guests arrive tomorrow), the review reminders (guests and hosts of stays that checked out
    /// yesterday and can still review them, with an e-mail) and the experience reminders (guests of tomorrow's
    /// experiences). Dates are UTC.
    /// </summary>
    /// <returns>The number of new reminders.</returns>
    Task<int> SendAsync(CancellationToken cancellationToken = default);
}
