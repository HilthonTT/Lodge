using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Experiences;

namespace Lodge.Application.Experiences;

/// <summary>
/// Gets one of the caller's experiences (listed or not).
/// </summary>
public sealed record GetHostExperienceQuery(Guid ExperienceId) : IQuery<ExperienceResponse>;
