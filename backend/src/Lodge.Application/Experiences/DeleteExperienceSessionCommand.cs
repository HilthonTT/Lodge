using Lodge.Application.Abstractions.Messaging;

namespace Lodge.Application.Experiences;

/// <summary>
/// Removes a session without bookings, or cancels a session with bookings and refunds every booking in full.
/// </summary>
public sealed record DeleteExperienceSessionCommand(Guid ExperienceId, Guid SessionId) : ICommand;
