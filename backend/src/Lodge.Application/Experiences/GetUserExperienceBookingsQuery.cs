using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Experiences;

namespace Lodge.Application.Experiences;

/// <summary>
/// Gets the experience bookings of the caller (<c>GET /users/{userId}/experience-bookings</c>), latest session first.
/// </summary>
public sealed record GetUserExperienceBookingsQuery(Guid UserId) : IQuery<List<ExperienceBookingResponse>>;
