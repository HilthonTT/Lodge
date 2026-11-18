using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Experiences;

namespace Lodge.Application.Experiences;

/// <summary>
/// Gets a listed experience; unlisted ones are only visible to their host (<paramref name="CallerId"/>).
/// </summary>
public sealed record GetExperienceByIdQuery(Guid ExperienceId, Guid? CallerId) : IQuery<ExperienceResponse>;
