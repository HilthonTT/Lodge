using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Apartments;
using Lodge.Contracts.Common;
using Lodge.Domain.Apartements;
using Lodge.Domain.Core.Primitives;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Linq.Expressions;

namespace Lodge.Application.Apartments.Search;

/// <summary>
/// Represents the <see cref="SearchApartmentQuery"/> handler.
/// </summary>
/// <param name="factory">The database connection.</param>
internal sealed class SearchApartmentQueryHandler(IDbContext context) 
    : IQueryHandler<SearchApartmentQuery, PagedList<ApartmentResponse>>
{
    /// <inheritdoc />
    public async Task<Result<PagedList<ApartmentResponse>>> Handle(
        SearchApartmentQuery request, 
        CancellationToken cancellationToken)
    {
        IQueryable<Apartment> apartmentsQuery = context
            .Set<Apartment>()
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            apartmentsQuery = apartmentsQuery
                .Where(a => 
                    (a.Name.Value).Contains(request.SearchTerm) ||
                    (a.Description.Value).Contains(request.SearchTerm));
        }

        Expression<Func<Apartment, object>> keySelector = GetSortProperty(request);

        if (request.SortOrder?.ToLower() == "desc")
        {
            apartmentsQuery = apartmentsQuery.OrderByDescending(keySelector);
        }
        else
        {
            apartmentsQuery = apartmentsQuery.OrderBy(keySelector);
        }

        List<ApartmentResponse> apartmentResponsesQuery = await apartmentsQuery
            .Select(a => new ApartmentResponse(
                a.Id,
                (string)a.Name,
                (string)a.Description,
                a.Price.Amount,
                a.Price.Currency.Code,
                a.ImageUrl,
                new AddressResponse(a.Address.Country, a.Address.State, a.Address.City, a.Address.Street)))
            .ToListAsync(cancellationToken);

        var apartments = PagedList<ApartmentResponse>.Create(apartmentResponsesQuery, request.Page, request.PageSize);

        return apartments;
    }

    /// <summary>
    /// Gets the sort property of the apartment.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <returns>An instance of <see cref="Func{T}"/>.</returns>
    private static Expression<Func<Apartment, object>> GetSortProperty(SearchApartmentQuery request) =>
        request.SortColumn?.ToLower() switch
        {
            "name" => apartment => apartment.Name,
            "description" => apartment => apartment.Description,
            "country" => apartment => apartment.Address.Country,
            "price_amount" => apartment => apartment.Price.Amount,
            "currency" => apartment => apartment.Price.Currency,
            _ => apartment => apartment.Id,
        };
}
