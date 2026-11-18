using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Common;
using Lodge.Contracts.Experiences;

namespace Lodge.Application.Experiences;

/// <summary>
/// Searches the listed experiences that have a future session with enough spots for <paramref name="Guests"/> (on
/// <paramref name="Date"/>, a UTC date, if given). Prices are compared in the base currency (USD). Ordered by the next
/// session.
/// </summary>
public sealed record SearchExperiencesQuery(
    string? Q,
    string? City,
    string? Category,
    DateOnly? Date,
    int? Guests,
    decimal? MinPrice,
    decimal? MaxPrice,
    int Page,
    int PageSize) : IQuery<PagedList<ExperienceSummaryResponse>>;
