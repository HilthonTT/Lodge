using System.Data;
using Dapper;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Application.Core.Extensions;
using Lodge.Application.Payments;
using Lodge.Contracts.Apartments;
using Lodge.Contracts.Common;
using Lodge.Domain.Apartments;
using Lodge.Domain.Bookings;
using Lodge.Domain.Core.Primitives;

namespace Lodge.Application.Apartments.Search;

/// <summary>
/// Builds one parameterized SQL statement from the filters (every value is a parameter; only fixed SQL fragments are
/// concatenated). Availability is checked with <c>NOT EXISTS</c> sub-queries on the bookings and the host calendar,
/// which use the <c>(apartment_id, ...)</c> indexes.
/// </summary>
internal sealed class SearchApartmentQueryHandler(
    IDbConnectionFactory factory,
    ApartmentReader apartmentReader,
    IDateTimeProvider dateTimeProvider,
    CurrencyConverter currencyConverter)
    : IQueryHandler<SearchApartmentQuery, PagedList<ApartmentResponse>>
{
    /// <summary>
    /// "The stay [@CheckIn, @CheckOut) is free": no active booking overlaps it and the host blocked none of its nights.
    /// </summary>
    private const string StayIsFree =
        """
        NOT EXISTS (
            SELECT 1 FROM bookings AS b
            WHERE b.apartment_id = a.id
              AND b.status = ANY(@ActiveStatuses)
              AND b.duration_start < {1}
              AND b.duration_end > {0})
        AND NOT EXISTS (
            SELECT 1 FROM apartment_calendar_days AS c
            WHERE c.apartment_id = a.id
              AND c.is_blocked
              AND c.date >= {0}
              AND c.date < {1})
        """;

    public async Task<Result<PagedList<ApartmentResponse>>> Handle(
        SearchApartmentQuery request,
        CancellationToken cancellationToken)
    {
        var parameters = new DynamicParameters();
        List<string> conditions = [ApartmentReader.ListedCondition];

        AddTextFilter(request, conditions, parameters);
        AddCapacityAndPriceFilters(request, conditions, parameters, currencyConverter.ToBaseSql("a.price_amount", "a.price_currency"));
        AddFeatureFilters(request, conditions, parameters);
        AddBoundsFilter(request, conditions, parameters);
        AddAvailabilityFilters(request, conditions, parameters, DateOnly.FromDateTime(dateTimeProvider.UtcNow));

        string where = $"WHERE {string.Join("\n  AND ", conditions)}";

        using IDbConnection connection = await factory.GetOpenConnectionAsync(cancellationToken);

        int totalCount = await connection.ExecuteScalarAsync<int>(
            $"""
            SELECT COUNT(*)
            FROM apartments AS a
            INNER JOIN users AS u ON u.id = a.owner_id
            {where}
            """,
            parameters);

        parameters.Add("PageSize", request.PageSize);
        parameters.Add("Offset", (request.Page - 1) * request.PageSize);

        List<ApartmentResponse> items = await apartmentReader.QueryAsync(
            connection,
            $"""
            {where}
            ORDER BY {OrderBy(request.SortColumn, request.SortOrder, currencyConverter.ToBaseSql("a.price_amount", "a.price_currency"))}
            LIMIT @PageSize OFFSET @Offset
            """,
            parameters);

        return new PagedList<ApartmentResponse>(items, request.Page, request.PageSize, totalCount);
    }

    private static void AddTextFilter(SearchApartmentQuery request, List<string> conditions, DynamicParameters parameters)
    {
        if (string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            return;
        }

        conditions.Add(
            """
            (unaccent(a.name) ILIKE unaccent(@Pattern)
                OR unaccent(a.address_city) ILIKE unaccent(@Pattern)
                OR unaccent(a.address_state) ILIKE unaccent(@Pattern)
                OR unaccent(a.address_country) ILIKE unaccent(@Pattern))
            """);
        parameters.Add("Pattern", SqlPatterns.Contains(request.SearchTerm));
    }

    /// <summary>
    /// Adds the capacity filters and the price filters; <c>minPrice</c>/<c>maxPrice</c> are in the base currency (USD):
    /// each listing's nightly price is converted with the static <c>Currencies:Rates</c> before comparing.
    /// </summary>
    private static void AddCapacityAndPriceFilters(
        SearchApartmentQuery request,
        List<string> conditions,
        DynamicParameters parameters,
        string basePrice)
    {
        AddMinimum(conditions, parameters, "a.maximum_guest_count", "GuestCount", request.GuestCount);
        AddMinimum(conditions, parameters, "a.bedrooms", "Bedrooms", request.Bedrooms);
        AddMinimum(conditions, parameters, "a.beds", "Beds", request.Beds);
        AddMinimum(conditions, parameters, "a.bathrooms", "Bathrooms", request.Bathrooms);
        AddMinimum(conditions, parameters, basePrice, "MinPrice", request.MinPrice);

        if (request.MaxPrice is decimal maxPrice)
        {
            conditions.Add($"{basePrice} <= @MaxPrice");
            parameters.Add("MaxPrice", maxPrice);
        }
    }

    private static void AddFeatureFilters(SearchApartmentQuery request, List<string> conditions, DynamicParameters parameters)
    {
        int[] amenities = [.. ParseAll<Amenity>(request.Amenities).Select(amenity => (int)amenity).Distinct()];

        if (amenities.Length > 0)
        {
            // Array containment: the apartment has every requested amenity (uses the GIN index).
            conditions.Add("a.amenities @> @Amenities");
            parameters.Add("Amenities", amenities);
        }

        int[] propertyTypes = [.. ParseAll<PropertyType>(request.PropertyTypes).Select(type => (int)type).Distinct()];

        if (propertyTypes.Length > 0)
        {
            conditions.Add("a.property_type = ANY(@PropertyTypes)");
            parameters.Add("PropertyTypes", propertyTypes);
        }

        if (request.InstantBook is bool instantBook)
        {
            conditions.Add("a.instant_book = @InstantBook");
            parameters.Add("InstantBook", instantBook);
        }

        if (request.Superhost is bool superhost)
        {
            conditions.Add("u.is_superhost = @Superhost");
            parameters.Add("Superhost", superhost);
        }
    }

    private static void AddBoundsFilter(SearchApartmentQuery request, List<string> conditions, DynamicParameters parameters)
    {
        AddMinimum(conditions, parameters, "a.latitude", "MinLatitude", request.MinLatitude);

        if (request.MaxLatitude is double maxLatitude)
        {
            conditions.Add("a.latitude <= @MaxLatitude");
            parameters.Add("MaxLatitude", maxLatitude);
        }

        if (request is { MinLongitude: double west, MaxLongitude: double east } && west > east)
        {
            // The map box crosses the antimeridian (e.g. 170 → -170).
            conditions.Add("(a.longitude >= @MinLongitude OR a.longitude <= @MaxLongitude)");
            parameters.Add("MinLongitude", west);
            parameters.Add("MaxLongitude", east);
            return;
        }

        AddMinimum(conditions, parameters, "a.longitude", "MinLongitude", request.MinLongitude);

        if (request.MaxLongitude is double maxLongitude)
        {
            conditions.Add("a.longitude <= @MaxLongitude");
            parameters.Add("MaxLongitude", maxLongitude);
        }
    }

    private static void AddAvailabilityFilters(
        SearchApartmentQuery request,
        List<string> conditions,
        DynamicParameters parameters,
        DateOnly today)
    {
        if (request is { StartDate: DateOnly checkIn, EndDate: DateOnly checkOut })
        {
            conditions.Add("a.min_nights <= @Nights AND a.max_nights >= @Nights");
            conditions.Add(string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                StayIsFree,
                "@CheckIn::date",
                "@CheckOut::date"));

            parameters.Add("Nights", checkOut.DayNumber - checkIn.DayNumber);
            parameters.Add("CheckIn", checkIn.ToDateTime(TimeOnly.MinValue));
            parameters.Add("CheckOut", checkOut.ToDateTime(TimeOnly.MinValue));
            parameters.Add("ActiveStatuses", BookingStatuses.ActiveValues);
        }

        if (request.Flexible is not null && FlexibleStay.TryParseLength(request.Flexible, out FlexibleStayLength length))
        {
            List<YearMonth> months = [];

            foreach (string value in request.Months)
            {
                if (YearMonth.TryParse(value, out YearMonth month))
                {
                    months.Add(month);
                }
            }

            if (months.Count == 0)
            {
                // Without months: the current month and the next two.
                var current = new YearMonth(today.Year, today.Month);
                months = [current, Next(current), Next(Next(current))];
            }

            IReadOnlyList<DateOnly> checkIns = FlexibleStay.CandidateCheckIns(length, months, today);

            if (checkIns.Count == 0)
            {
                conditions.Add("FALSE");
                return;
            }

            // At least one candidate check-in date starts a free window of the requested length.
            conditions.Add("a.min_nights <= @FlexibleNights AND a.max_nights >= @FlexibleNights");
            conditions.Add(
                "EXISTS (SELECT 1 FROM unnest(@FlexibleCheckIns::date[]) AS s(check_in) WHERE " +
                string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    StayIsFree,
                    "s.check_in",
                    "s.check_in + @FlexibleNights") +
                ")");

            parameters.Add("FlexibleNights", FlexibleStay.Nights(length));
            parameters.Add("FlexibleCheckIns", checkIns.Select(date => date.ToDateTime(TimeOnly.MinValue)).ToArray());
            parameters.Add("ActiveStatuses", BookingStatuses.ActiveValues);
        }
    }

    private static YearMonth Next(YearMonth month) =>
        month.Month == 12 ? new YearMonth(month.Year + 1, 1) : new YearMonth(month.Year, month.Month + 1);

    private static void AddMinimum<T>(
        List<string> conditions,
        DynamicParameters parameters,
        string column,
        string parameterName,
        T? value)
        where T : struct
    {
        if (value is T minimum)
        {
            conditions.Add($"{column} >= @{parameterName}");
            parameters.Add(parameterName, minimum);
        }
    }

    private static IEnumerable<TEnum> ParseAll<TEnum>(IEnumerable<string> names)
        where TEnum : struct, Enum
    {
        foreach (string name in names)
        {
            if (EnumNames.TryParse(name, out TEnum value))
            {
                yield return value;
            }
        }
    }

    /// <summary>
    /// Maps the sort column to a fixed SQL expression (never user input). Unrated apartments come last.
    /// </summary>
    private static string OrderBy(string? sortColumn, string? sortOrder, string basePrice)
    {
        string direction = string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";

        string expression = sortColumn?.ToLowerInvariant() switch
        {
            "rating" => "rs.average_rating",
            "description" => "a.description",
            "country" => "a.address_country",
            "price" or "price_amount" => basePrice,
            "currency" => "a.price_currency",
            "newest" => "a.created_on_utc",
            _ => "a.name",
        };

        return $"{expression} {direction} NULLS LAST, a.id";
    }
}
