using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Common;
using Lodge.Contracts.Experiences;

namespace Lodge.Application.Experiences;

/// <summary>
/// Gets one page of the caller's experiences, newest first (<c>GET /hosting/experiences</c>).
/// </summary>
public sealed record GetHostExperiencesQuery(int Page, int PageSize) : IQuery<PagedList<HostExperienceSummaryResponse>>;
