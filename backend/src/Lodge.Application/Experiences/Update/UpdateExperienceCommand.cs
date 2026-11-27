using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Experiences;

namespace Lodge.Application.Experiences.Update;

/// <summary>
/// Replaces the details and photos of an experience.
/// </summary>
public sealed record UpdateExperienceCommand(Guid ExperienceId, ExperienceRequest Experience) : ICommand;
