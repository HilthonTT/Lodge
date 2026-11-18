using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Experiences;

namespace Lodge.Application.Experiences;

/// <summary>
/// Gets an experience booking: its guest and the experience's host only (others get 404).
/// </summary>
public sealed record GetExperienceBookingQuery(Guid ExperienceBookingId) : IQuery<ExperienceBookingResponse>;
