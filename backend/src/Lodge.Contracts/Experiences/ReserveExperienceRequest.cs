namespace Lodge.Contracts.Experiences;

/// <summary>
/// Books spots in a session (<c>POST /experience-bookings</c>, with an <c>X-Idempotency-Key</c> header).
/// </summary>
public sealed record ReserveExperienceRequest(Guid SessionId, int GuestCount);
