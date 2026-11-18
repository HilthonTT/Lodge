using System.Data;
using Dapper;
using Lodge.Application.Abstractions.App;
using Lodge.Application.Abstractions.Assets;
using Lodge.Contracts.Apartments;
using Lodge.Domain.Apartments;
using Microsoft.Extensions.Options;

namespace Lodge.Application.Apartments;

/// <summary>
/// Reads <see cref="ApartmentResponse"/>s with Dapper: one query for the apartments (with their host and review
/// statistics) and one for the photos of all of them, so lists never cause N+1 queries.
/// </summary>
internal sealed class ApartmentReader(IAssetUrlResolver assetUrlResolver, IOptions<AppOptions> appOptions)
{
    /// <summary>
    /// The condition for apartments that guests may see.
    /// </summary>
    public const string ListedCondition = "a.is_listed AND NOT a.deleted";

    /// <summary>
    /// Selects every column of <see cref="ApartmentRow"/>. Callers append joins, conditions, ordering and paging.
    /// The table alias is <c>a</c>, the host is <c>u</c> and the review statistics are <c>rs</c>.
    /// </summary>
    public const string SelectApartment =
        """
        SELECT
            a.id AS Id,
            a.owner_id AS OwnerId,
            a.name AS Name,
            a.description AS Description,
            a.property_type AS PropertyType,
            a.price_amount AS Price,
            a.price_currency AS Currency,
            a.cleaning_fee_amount AS CleaningFee,
            a.weekend_price AS WeekendPrice,
            a.weekly_discount_percent AS WeeklyDiscountPercent,
            a.monthly_discount_percent AS MonthlyDiscountPercent,
            a.bedrooms AS Bedrooms,
            a.beds AS Beds,
            a.bathrooms AS Bathrooms,
            a.maximum_guest_count AS MaximumGuestCount,
            a.latitude AS Latitude,
            a.longitude AS Longitude,
            a.amenities AS Amenities,
            to_char(a.check_in_time, 'HH24:MI') AS CheckInTime,
            to_char(a.check_out_time, 'HH24:MI') AS CheckOutTime,
            a.pets_allowed AS PetsAllowed,
            a.smoking_allowed AS SmokingAllowed,
            a.parties_allowed AS PartiesAllowed,
            a.house_rules AS HouseRules,
            a.check_in_instructions AS CheckInInstructions,
            a.instant_book AS InstantBook,
            a.min_nights AS MinNights,
            a.max_nights AS MaxNights,
            a.is_listed AS IsListed,
            a.created_on_utc AS CreatedOnUtc,
            a.modified_on_utc AS ModifiedOnUtc,
            a.address_country AS Country,
            a.address_state AS State,
            a.address_zip_code AS ZipCode,
            a.address_city AS City,
            a.address_street AS Street,
            rs.average_rating AS AverageRating,
            rs.review_count AS ReviewCount,
            u.first_name AS HostFirstName,
            u.image_id AS HostImageId,
            u.is_superhost AS HostIsSuperhost,
            u.created_on_utc AS HostJoinedOnUtc
        FROM apartments AS a
        INNER JOIN users AS u ON u.id = a.owner_id
        LEFT JOIN LATERAL (
            SELECT
                ROUND(AVG(r.rating)::numeric, 1) AS average_rating,
                COUNT(*)::int AS review_count
            FROM reviews AS r
            WHERE r.apartment_id = a.id
        ) AS rs ON TRUE
        """;

    /// <summary>
    /// Runs <see cref="SelectApartment"/> followed by <paramref name="sqlSuffix"/> and maps the rows.
    /// </summary>
    public async Task<List<ApartmentResponse>> QueryAsync(IDbConnection connection, string sqlSuffix, object? parameters)
    {
        List<ApartmentRow> rows = (await connection.QueryAsync<ApartmentRow>($"{SelectApartment}\n{sqlSuffix}", parameters))
            .ToList();

        Dictionary<Guid, List<ImageRow>> images = await LoadImagesAsync(connection, rows);

        return [.. rows.Select(row => Map(row, images, new ApartmentResponse
        {
            Id = row.Id,
            Name = row.Name,
            Description = row.Description,
            PropertyType = PropertyTypeName(row.PropertyType),
            Price = row.Price,
            Currency = row.Currency,
            CleaningFee = row.CleaningFee,
            WeekendPrice = row.WeekendPrice,
            WeeklyDiscountPercent = row.WeeklyDiscountPercent,
            MonthlyDiscountPercent = row.MonthlyDiscountPercent,
            Bedrooms = row.Bedrooms,
            Beds = row.Beds,
            Bathrooms = row.Bathrooms,
            MaximumGuestCount = row.MaximumGuestCount,
            Latitude = row.Latitude,
            Longitude = row.Longitude,
            Amenities = AmenityNames(row.Amenities),
            CheckInTime = row.CheckInTime,
            CheckOutTime = row.CheckOutTime,
            PetsAllowed = row.PetsAllowed,
            SmokingAllowed = row.SmokingAllowed,
            PartiesAllowed = row.PartiesAllowed,
            HouseRules = row.HouseRules,
            InstantBook = row.InstantBook,
            MinNights = row.MinNights,
            MaxNights = row.MaxNights,
        }))];
    }

    /// <summary>
    /// Reads a (not deleted) listing with its private fields, listed or not.
    /// </summary>
    public async Task<HostListingResponse?> GetHostListingAsync(IDbConnection connection, Guid apartmentId)
    {
        List<ApartmentRow> rows = (await connection.QueryAsync<ApartmentRow>(
                $"{SelectApartment}\nWHERE a.id = @ApartmentId AND NOT a.deleted",
                new { ApartmentId = apartmentId }))
            .ToList();

        if (rows.Count == 0)
        {
            return null;
        }

        ApartmentRow row = rows[0];
        Dictionary<Guid, List<ImageRow>> images = await LoadImagesAsync(connection, rows);

        return Map(row, images, new HostListingResponse
        {
            Id = row.Id,
            Name = row.Name,
            Description = row.Description,
            PropertyType = PropertyTypeName(row.PropertyType),
            Price = row.Price,
            Currency = row.Currency,
            CleaningFee = row.CleaningFee,
            WeekendPrice = row.WeekendPrice,
            WeeklyDiscountPercent = row.WeeklyDiscountPercent,
            MonthlyDiscountPercent = row.MonthlyDiscountPercent,
            Bedrooms = row.Bedrooms,
            Beds = row.Beds,
            Bathrooms = row.Bathrooms,
            MaximumGuestCount = row.MaximumGuestCount,
            Latitude = row.Latitude,
            Longitude = row.Longitude,
            Amenities = AmenityNames(row.Amenities),
            CheckInTime = row.CheckInTime,
            CheckOutTime = row.CheckOutTime,
            PetsAllowed = row.PetsAllowed,
            SmokingAllowed = row.SmokingAllowed,
            PartiesAllowed = row.PartiesAllowed,
            HouseRules = row.HouseRules,
            InstantBook = row.InstantBook,
            MinNights = row.MinNights,
            MaxNights = row.MaxNights,
            CheckInInstructions = row.CheckInInstructions,
            IsListed = row.IsListed,
            CreatedOnUtc = row.CreatedOnUtc,
            ModifiedOnUtc = row.ModifiedOnUtc,
        });
    }

    /// <summary>
    /// Gets the absolute URL of a photo: uploaded files are served by <c>/api/v1/files/{id}</c>, bundled assets by
    /// the API's static files.
    /// </summary>
    public string ImageUrl(Guid imageId, string? path) =>
        path is null ? appOptions.Value.FileUrl(imageId) : assetUrlResolver.Resolve(path);

    public string? AvatarUrl(Guid? imageId) => imageId is Guid id ? appOptions.Value.FileUrl(id) : null;

    public static string PropertyTypeName(int propertyType) =>
        Enum.IsDefined((PropertyType)propertyType) ? ((PropertyType)propertyType).ToString() : nameof(Domain.Apartments.PropertyType.Other);

    public static IReadOnlyList<string> AmenityNames(int[]? amenities) =>
        amenities is null
            ? []
            : [.. amenities.Select(value => (Amenity)value).Where(Enum.IsDefined).Select(amenity => amenity.ToString())];

    private static async Task<Dictionary<Guid, List<ImageRow>>> LoadImagesAsync(
        IDbConnection connection,
        List<ApartmentRow> rows)
    {
        if (rows.Count == 0)
        {
            return [];
        }

        IEnumerable<ImageRow> images = await connection.QueryAsync<ImageRow>(
            """
            SELECT i.apartment_id AS ApartmentId, i.id AS Id, i.path AS Path
            FROM apartment_images AS i
            WHERE i.apartment_id = ANY(@ApartmentIds)
            ORDER BY i.apartment_id, i.position
            """,
            new { ApartmentIds = rows.Select(row => row.Id).Distinct().ToArray() });

        return images.GroupBy(image => image.ApartmentId).ToDictionary(group => group.Key, group => group.ToList());
    }

    private T Map<T>(ApartmentRow row, Dictionary<Guid, List<ImageRow>> images, T response)
        where T : ApartmentResponse
    {
        List<ApartmentImageResponse> photos = images.TryGetValue(row.Id, out List<ImageRow>? imageRows)
            ? [.. imageRows.Select(image => new ApartmentImageResponse(image.Id, ImageUrl(image.Id, image.Path)))]
            : [];

        response.Images = photos;
        response.ImageUrl = photos.Count > 0 ? photos[0].Url : string.Empty;
        response.AverageRating = row.AverageRating;
        response.ReviewCount = row.ReviewCount;
        response.Address = new AddressResponse
        {
            Country = row.Country,
            State = row.State,
            ZipCode = row.ZipCode,
            City = row.City,
            Street = row.Street,
        };
        response.Host = new HostSummaryResponse(
            row.OwnerId,
            row.HostFirstName,
            AvatarUrl(row.HostImageId),
            row.HostIsSuperhost,
            row.HostJoinedOnUtc);

        return response;
    }

    internal sealed class ApartmentRow
    {
        public Guid Id { get; init; }

        public Guid OwnerId { get; init; }

        public string Name { get; init; } = string.Empty;

        public string Description { get; init; } = string.Empty;

        public int PropertyType { get; init; }

        public decimal Price { get; init; }

        public string Currency { get; init; } = string.Empty;

        public decimal CleaningFee { get; init; }

        public decimal? WeekendPrice { get; init; }

        public int WeeklyDiscountPercent { get; init; }

        public int MonthlyDiscountPercent { get; init; }

        public int Bedrooms { get; init; }

        public int Beds { get; init; }

        public decimal Bathrooms { get; init; }

        public int MaximumGuestCount { get; init; }

        public double Latitude { get; init; }

        public double Longitude { get; init; }

        public int[]? Amenities { get; init; }

        public string CheckInTime { get; init; } = string.Empty;

        public string CheckOutTime { get; init; } = string.Empty;

        public bool PetsAllowed { get; init; }

        public bool SmokingAllowed { get; init; }

        public bool PartiesAllowed { get; init; }

        public string? HouseRules { get; init; }

        public string? CheckInInstructions { get; init; }

        public bool InstantBook { get; init; }

        public int MinNights { get; init; }

        public int MaxNights { get; init; }

        public bool IsListed { get; init; }

        public DateTime CreatedOnUtc { get; init; }

        public DateTime? ModifiedOnUtc { get; init; }

        public string Country { get; init; } = string.Empty;

        public string State { get; init; } = string.Empty;

        public string ZipCode { get; init; } = string.Empty;

        public string City { get; init; } = string.Empty;

        public string Street { get; init; } = string.Empty;

        public decimal? AverageRating { get; init; }

        public int ReviewCount { get; init; }

        public string HostFirstName { get; init; } = string.Empty;

        public Guid? HostImageId { get; init; }

        public bool HostIsSuperhost { get; init; }

        public DateTime HostJoinedOnUtc { get; init; }
    }

    internal sealed class ImageRow
    {
        public Guid ApartmentId { get; init; }

        public Guid Id { get; init; }

        public string? Path { get; init; }
    }
}
