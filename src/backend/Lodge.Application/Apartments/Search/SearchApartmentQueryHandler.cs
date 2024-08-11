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
        IQueryable<Apartment> apartmentsQuery = context.Set<Apartment>().AsNoTracking();

        // Filter by Search Term
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            apartmentsQuery = apartmentsQuery
                .Where(a => a.Address.Country.Contains(request.SearchTerm));
        }

        // Filter by Guest Count
        if (request.GuestCount.HasValue && request.GuestCount.GetValueOrDefault() >= 1)
        {
            apartmentsQuery = apartmentsQuery
                .Where(a => a.MaximumGuestCount >= request.GuestCount.GetValueOrDefault());
        }

        // Filter by Room Count
        if (request.RoomCount.HasValue && request.RoomCount.GetValueOrDefault() >= 1)
        {
            apartmentsQuery = apartmentsQuery
                .Where(a => a.MaximumRoomCount >= request.RoomCount.GetValueOrDefault());
        }

        // Sorting
        Expression<Func<Apartment, object>> keySelector = GetSortProperty(request);
        apartmentsQuery = request.SortOrder?.ToLower() == "desc"
            ? apartmentsQuery.OrderByDescending(keySelector)
            : apartmentsQuery.OrderBy(keySelector);

        // Fetch and Project to Response
        List<ApartmentResponse> apartmentResponsesQuery = await apartmentsQuery
            .Select(a => new ApartmentResponse
            {
                Id = a.Id,
                Name = a.Name.Value,
                Description = a.Description.Value,
                Price = a.Price.Amount,
                Currency = a.Price.Currency.Code,
                ImageUrl = a.ImageUrl,
                Address = new AddressResponse 
                { 
                    Country = a.Address.Country,
                    State = a.Address.State,
                    City = a.Address.City,
                    Street = a.Address.Street 
                }
            })
            .ToListAsync(cancellationToken);

        // Paginate Results
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
            "name" => apartment => apartment.Name.Value,
            "description" => apartment => apartment.Description.Value,
            "country" => apartment => apartment.Address.Country,
            "price_amount" => apartment => apartment.Price.Amount,
            "currency" => apartment => apartment.Price.Currency.Code,
            _ => apartment => apartment.Id,
        };
}
