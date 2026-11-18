using Lodge.Application.Abstractions.Messaging;

namespace Lodge.Application.Experiences;

/// <summary>
/// The guest cancels an experience booking before the session: a full refund at least 24 hours before the start,
/// otherwise none (the refund is sent by the background refund pipeline).
/// </summary>
public sealed record CancelExperienceBookingCommand(Guid ExperienceBookingId) : ICommand;
