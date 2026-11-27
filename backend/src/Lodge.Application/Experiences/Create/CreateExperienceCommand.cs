using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Experiences;

namespace Lodge.Application.Experiences.Create;

/// <summary>
/// Creates an experience hosted by the caller (who needs a verified e-mail address).
/// </summary>
public sealed record CreateExperienceCommand(ExperienceRequest Experience) : ICommand<Guid>;
