using Lodge.Application.Abstractions.Messaging;

namespace Lodge.Application.Experiences;

/// <summary>
/// Lists or unlists an experience.
/// </summary>
public sealed record SetExperienceStatusCommand(Guid ExperienceId, bool IsListed) : ICommand;
