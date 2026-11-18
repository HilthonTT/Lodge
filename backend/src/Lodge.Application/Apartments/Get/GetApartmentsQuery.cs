using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Apartments;
using Lodge.Contracts.Common;

namespace Lodge.Application.Apartments.Get;

/// <summary>
/// Represents the query for fetching the apartments.
/// </summary>
/// <param name="Page">The current page.</param>
/// <param name="PageSize">The page size.</param>
public sealed record GetApartmentsQuery(int Page, int PageSize) : IQuery<PagedList<ApartmentResponse>>;
