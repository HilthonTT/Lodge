using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Experiences;

namespace Lodge.Application.Experiences.GetSessions;

/// <summary>
/// Gets the bookable sessions of a listed experience between <paramref name="From"/> (default: now) and
/// <paramref name="To"/> (default: 60 days later, at most 366 days).
/// </summary>
public sealed record GetExperienceSessionsQuery(Guid ExperienceId, DateTime? From, DateTime? To)
    : IQuery<List<ExperienceSessionResponse>>;
