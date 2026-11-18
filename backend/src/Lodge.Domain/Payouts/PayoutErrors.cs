using Lodge.Domain.Core.Primitives;

namespace Lodge.Domain.Payouts;

/// <summary>
/// Contains the payout errors.
/// </summary>
public static class PayoutErrors
{
    public static readonly Error NotPayable = Error.Problem(
        "Payout.NotPayable",
        "Only scheduled payouts or payouts on hold can be paid");
}
