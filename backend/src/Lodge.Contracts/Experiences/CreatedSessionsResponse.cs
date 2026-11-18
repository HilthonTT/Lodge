namespace Lodge.Contracts.Experiences;

/// <summary>
/// The ids of the created sessions.
/// </summary>
public sealed record CreatedSessionsResponse(IReadOnlyList<Guid> Ids);
