using System.Linq.Expressions;
using Lodge.Application.Abstractions.Assets;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Apartments;
using Lodge.Contracts.Common;
using Lodge.Domain.Apartments;
using Lodge.Domain.Bookings;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Reviews;
using Microsoft.EntityFrameworkCore;

namespace Lodge.Application.Apartments.Search;

internal sealed class SearchApartmentQueryHandler(IDbContext context, IAssetUrlResolver assetUrlResolver)
    : IQueryHandler<SearchApartmentQuery, PagedList<ApartmentResponse>>
{
    private static readonly BookingStatus[] ActiveBookingStatuses =
    [
        BookingStatus.Reserved,
        BookingStatus.Confirmed,
        BookingStatus.Completed
    ];

    public async Task<Result<PagedList<ApartmentResponse>>> Handle(
        SearchApartmentQuery request,
        CancellationToken cancellationToken)
    {
        IQueryable<Apartment> apartmentsQuery = context.Set<Apartment>().AsNoTracking();
        IQueryable<Review> reviews = context.Set<Review>();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            string searchTerm = request.SearchTerm.Trim().ToLower();

            apartmentsQuery = apartmentsQuery.Where(a =>
                a.Address.Country.ToLower().Contains(searchTerm) ||
                a.Address.City.ToLower().Contains(searchTerm) ||
                a.Name.Value.ToLower().Contains(searchTerm));
        }

        if (request.GuestCount is >= 1)
        {
            apartmentsQuery = apartmentsQuery.Where(a => a.MaximumGuestCount >= request.GuestCount.Value);
        }

        if (request.RoomCount is >= 1)
        {
            apartmentsQuery = apartmentsQuery.Where(a => a.MaximumRoomCount >= request.RoomCount.Value);
        }

        // Only show apartments that are available for the whole requested period
        // (same night-based overlap rule as reservations).
        if (request is { StartDate: DateOnly startDate, EndDate: DateOnly endDate })
        {
            IQueryable<Booking> bookings = context.Set<Booking>();

            apartmentsQuery = apartmentsQuery.Where(a => !bookings.Any(b =>
                b.ApartmentId == a.Id &&
                ActiveBookingStatuses.Contains(b.Status) &&
                b.Duration.Start < endDate &&
                b.Duration.End > startDate));
        }

        int totalCount = await apartmentsQuery.CountAsync(cancellationToken);

        bool descending = string.Equals(request.SortOrder, "desc", StringComparison.OrdinalIgnoreCase);

        List<ApartmentResponse> items = await Sort(apartmentsQuery, reviews, request.SortColumn, descending)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(a => new ApartmentResponse
            {
                Id = a.Id,
                Name = a.Name.Value,
                Description = a.Description.Value,
                Price = a.Price.Amount,
                Currency = a.Price.Currency.Code,
                ImageUrl = a.ImageUrl,
                MaximumRoomCount = a.MaximumRoomCount,
                MaximumGuestCount = a.MaximumGuestCount,
                AverageRating = reviews.Where(r => r.ApartmentId == a.Id).Average(r => (decimal?)r.Rating.Value),
                ReviewCount = reviews.Count(r => r.ApartmentId == a.Id),
                Address = new AddressResponse
                {
                    Country = a.Address.Country,
                    State = a.Address.State,
                    City = a.Address.City,
                    Street = a.Address.Street
                }
            })
            .ToListAsync(cancellationToken);

        foreach (ApartmentResponse apartment in items)
        {
            apartment.ImageUrl = assetUrlResolver.Resolve(apartment.ImageUrl);

            // Same rounding as PostgreSQL's ROUND(numeric, 1) used by the Dapper queries.
            apartment.AverageRating = apartment.AverageRating is decimal average
                ? Math.Round(average, 1, MidpointRounding.AwayFromZero)
                : null;
        }

        return new PagedList<ApartmentResponse>(items, request.Page, request.PageSize, totalCount);
    }

    private static IOrderedQueryable<Apartment> Sort(
        IQueryable<Apartment> query,
        IQueryable<Review> reviews,
        string? sortColumn,
        bool descending) =>
        sortColumn?.ToLowerInvariant() switch
        {
            "rating" => SortByRating(query, reviews, descending),
            "name" => OrderBy(query, a => a.Name.Value, descending),
            "description" => OrderBy(query, a => a.Description.Value, descending),
            "country" => OrderBy(query, a => a.Address.Country, descending),
            "price_amount" => OrderBy(query, a => a.Price.Amount, descending),
            "currency" => OrderBy(query, a => a.Price.Currency, descending),
            _ => OrderBy(query, a => a.Name.Value, descending)
        };

    /// <summary>
    /// Sorts by average rating; apartments without reviews come last in both directions.
    /// </summary>
    private static IOrderedQueryable<Apartment> SortByRating(
        IQueryable<Apartment> query,
        IQueryable<Review> reviews,
        bool descending)
    {
        IOrderedQueryable<Apartment> unratedLast = query.OrderBy(a => !reviews.Any(r => r.ApartmentId == a.Id));

        Expression<Func<Apartment, decimal?>> averageRating =
            a => reviews.Where(r => r.ApartmentId == a.Id).Average(r => (decimal?)r.Rating.Value);

        return (descending ? unratedLast.ThenByDescending(averageRating) : unratedLast.ThenBy(averageRating))
            .ThenBy(a => a.Id);
    }

    private static IOrderedQueryable<Apartment> OrderBy<TKey>(
        IQueryable<Apartment> query,
        Expression<Func<Apartment, TKey>> keySelector,
        bool descending) =>
        (descending ? query.OrderByDescending(keySelector) : query.OrderBy(keySelector))
            .ThenBy(a => a.Id);
}
