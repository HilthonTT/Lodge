using Lodge.Application.Abstractions.Idempotency;

namespace Lodge.Application.Experiences;

/// <summary>
/// Reserves spots in a session of a listed experience (verified e-mail required). The spots are held until the booking
/// is paid (<c>POST /experience-bookings/{id}/checkout</c>) or the hold expires. The session row is locked while the
/// spots are counted, so concurrent bookings never overbook it.
/// </summary>
public sealed record ReserveExperienceCommand(Guid RequestId, Guid SessionId, int GuestCount)
    : IdempotentCommand<Guid>(RequestId);
