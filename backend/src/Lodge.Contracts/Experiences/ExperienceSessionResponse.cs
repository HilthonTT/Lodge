namespace Lodge.Contracts.Experiences;

/// <summary>
/// A bookable session (<c>GET /experiences/{id}/sessions</c>).
/// </summary>
public sealed record ExperienceSessionResponse(Guid Id, DateTime StartsAtUtc, int Capacity, int SpotsLeft);
