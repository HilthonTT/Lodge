namespace Lodge.Domain.Payouts;

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
