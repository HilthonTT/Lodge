using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Apartments;
using Lodge.Contracts.Common;

namespace Lodge.Application.Apartments.Search;

/// <summary>
/// Represents the search apartment query.
/// </summary>
/// <param name="SearchTerm">The search term.</param>
/// <param name="SortColumn">The sort column.</param>
/// <param name="SortOrder">The sort order.</param>
/// <param name="Page">The current page.</param>
/// <param name="PageSize">The page size.</param>
/// <param name="StartDate">The start date.</param>
/// <param name="EndDate">The end date.</param>
public sealed record SearchApartmentQuery(
    string? SearchTerm,
    string? SortColumn,
    string? SortOrder,
    int Page,
    int PageSize,
    DateOnly StartDate, 
    DateOnly EndDate) 
    : IQuery<PagedList<ApartmentResponse>>;
