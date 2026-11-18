namespace Lodge.Domain.Payouts;

/// <summary>
/// The status of a host payout (stored and serialized by name).
/// </summary>
public enum PayoutStatus
{
    /// <summary>
    /// Waiting for its date (check-in + 1 day).
    /// </summary>
    Scheduled = 1,

    /// <summary>
    /// Due, but the host has no active payout account; it is paid once they connect one.
    /// </summary>
    OnHold = 2,

    Paid = 3,

    Failed = 4,

    /// <summary>
    /// The booking was cancelled with a full refund (nothing left for the host).
    /// </summary>
    Cancelled = 5,
}

/// <summary>
/// The status of a host's payout account (Stripe Connect Express), serialized by name.
/// </summary>
public enum PayoutAccountStatus
{
    NotConnected = 0,

    /// <summary>
    /// Onboarding started but payouts are not enabled yet.
    /// </summary>
    Pending = 1,

    Active = 2,
}
