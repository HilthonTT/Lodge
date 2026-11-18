namespace Lodge.Contracts.Experiences;

/// <summary>
/// Creates one session (<see cref="StartsAtUtc"/> + <see cref="Capacity"/>) or a recurring schedule
/// (<see cref="Recurring"/>). The capacity defaults to the experience's maximum number of guests.
/// </summary>
public sealed record CreateSessionsRequest
{
    public DateTime? StartsAtUtc { get; init; }

    public int? Capacity { get; init; }

    public RecurringSessionsRequest? Recurring { get; init; }
}
