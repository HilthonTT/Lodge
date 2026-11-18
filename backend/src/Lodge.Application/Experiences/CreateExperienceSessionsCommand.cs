using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Experiences;

namespace Lodge.Application.Experiences;

/// <summary>
/// Creates sessions of an experience: one (<c>startsAtUtc</c>) or a weekly schedule (<c>recurring</c>). Past start times
/// and start times that already have a session are skipped; at most 200 sessions are created at once.
/// </summary>
public sealed record CreateExperienceSessionsCommand(Guid ExperienceId, CreateSessionsRequest Sessions)
    : ICommand<CreatedSessionsResponse>;
