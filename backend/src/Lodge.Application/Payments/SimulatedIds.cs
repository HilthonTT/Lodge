namespace Lodge.Application.Payments;

/// <summary>
/// Ids created in simulated mode start with <see cref="Prefix"/>; money tied to them never goes through Stripe.
/// </summary>
internal static class SimulatedIds
{
    public const string Prefix = "sim_";

    public static string New(string kind) => $"{Prefix}{kind}_{Guid.NewGuid():N}";

    public static bool IsSimulated(string? id) => id?.StartsWith(Prefix, StringComparison.Ordinal) == true;
}
