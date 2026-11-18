using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Common;
using Lodge.Contracts.Experiences;

namespace Lodge.Application.Experiences;

/// <summary>
/// Gets one page of an experience's reviews, newest first.
/// </summary>
public sealed record GetExperienceReviewsQuery(Guid ExperienceId, int Page, int PageSize)
    : IQuery<PagedList<ExperienceReviewResponse>>;
