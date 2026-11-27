using Lodge.Application.Abstractions.Messaging;

namespace Lodge.Application.Experiences.Delete;

/// <summary>
/// Deletes (soft) an experience without upcoming reserved or confirmed bookings (409 otherwise).
/// </summary>
public sealed record DeleteExperienceCommand(Guid ExperienceId) : ICommand;
