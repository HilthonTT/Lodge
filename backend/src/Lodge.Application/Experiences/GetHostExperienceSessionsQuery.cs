using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Experiences;

namespace Lodge.Application.Experiences;

/// <summary>
/// Gets the sessions of one of the caller's experiences between <paramref name="From"/> (default: now) and
/// <paramref name="To"/> (default: 60 days later), including cancelled ones.
/// </summary>
public sealed record GetHostExperienceSessionsQuery(Guid ExperienceId, DateTime? From, DateTime? To)
    : IQuery<List<HostExperienceSessionResponse>>;
