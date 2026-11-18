using System.Globalization;
using Lodge.Application.Abstractions.Storage;
using Lodge.Application.Core.Extensions;
using Lodge.Contracts.Hosting;
using Lodge.Domain.Apartments;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Shared;

namespace Lodge.Application.Hosting.Listings;

/// <summary>
/// Turns a validated <see cref="ListingRequest"/> into domain values.
/// </summary>
internal static class ListingMapper
{
    public static bool TryParseTime(string? value, out TimeOnly time) =>
        TimeOnly.TryParseExact(value?.Trim(), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out time);

    public static bool IsSupportedCurrency(string? code) =>
        Currency.All.Any(currency => string.Equals(currency.Code, code?.Trim(), StringComparison.OrdinalIgnoreCase));

    public static Result<ListingDetails> ToDetails(ListingRequest request)
    {
        Result<Name> name = Name.Create(request.Name?.Trim());

        if (name.IsFailure)
        {
            return Result.Failure<ListingDetails>(name.Error);
        }

        Result<Description> description = Description.Create(request.Description?.Trim());

        if (description.IsFailure)
        {
            return Result.Failure<ListingDetails>(description.Error);
        }

        Currency currency = Currency.FromCode(request.Currency.Trim().ToUpperInvariant());

        TryParseTime(request.CheckInTime, out TimeOnly checkInTime);
        TryParseTime(request.CheckOutTime, out TimeOnly checkOutTime);

        return new ListingDetails
        {
            Name = name.Value,
            Description = description.Value,
            PropertyType = EnumNames.Parse<PropertyType>(request.PropertyType),
            Address = new Address(
                request.Address.Country.Trim(),
                request.Address.State.Trim(),
                request.Address.ZipCode.Trim(),
                request.Address.City.Trim(),
                request.Address.Street.Trim()),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Price = new Money(request.Price, currency),
            CleaningFee = new Money(request.CleaningFee, currency),
            WeekendPrice = request.WeekendPrice,
            WeeklyDiscountPercent = request.WeeklyDiscountPercent,
            MonthlyDiscountPercent = request.MonthlyDiscountPercent,
            Bedrooms = request.Bedrooms,
            Beds = request.Beds,
            Bathrooms = request.Bathrooms,
            MaximumGuestCount = request.MaximumGuestCount,
            Amenities = [.. request.Amenities.Select(EnumNames.Parse<Amenity>)],
            CheckInTime = checkInTime,
            CheckOutTime = checkOutTime,
            PetsAllowed = request.PetsAllowed,
            SmokingAllowed = request.SmokingAllowed,
            PartiesAllowed = request.PartiesAllowed,
            HouseRules = request.HouseRules,
            CheckInInstructions = request.CheckInInstructions,
            InstantBook = request.InstantBook,
            CancellationPolicy = EnumNames.Parse<CancellationPolicy>(request.CancellationPolicy),
            MinNights = request.MinNights,
            MaxNights = request.MaxNights,
        };
    }

    /// <summary>
    /// Resolves the photo ids: photos already on the listing are kept, other ids must be files uploaded by the user.
    /// </summary>
    public static async Task<Result<List<ApartmentImage>>> ResolveImagesAsync(
        IBlobService blobService,
        Apartment? apartment,
        IReadOnlyList<Guid> imageIds,
        Guid userId,
        CancellationToken cancellationToken)
    {
        List<ApartmentImage> images = [];

        foreach (Guid imageId in imageIds)
        {
            ApartmentImage? existing = apartment?.Images.FirstOrDefault(image => image.Id == imageId);

            if (existing is not null)
            {
                images.Add(existing);
                continue;
            }

            if (imageId == Guid.Empty || !await blobService.IsUserOwnerAsync(imageId, userId, cancellationToken))
            {
                return Result.Failure<List<ApartmentImage>>(ApartmentErrors.InvalidImage(imageId));
            }

            images.Add(ApartmentImage.FromFile(imageId));
        }

        return images;
    }
}
