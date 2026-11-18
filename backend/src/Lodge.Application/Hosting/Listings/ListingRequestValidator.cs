using FluentValidation;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;
using Lodge.Contracts.Hosting;
using Lodge.Domain.Apartments;

namespace Lodge.Application.Hosting.Listings;

/// <summary>
/// Validates a <see cref="ListingRequest"/> (used by the create and update listing validators).
/// </summary>
internal sealed class ListingRequestValidator : AbstractValidator<ListingRequest>
{
    public const decimal MaxPrice = 100_000;

    public const int AddressPartMaxLength = 200;

    public ListingRequestValidator()
    {
        RuleFor(x => x.Name)
            .Must(name => !string.IsNullOrWhiteSpace(name)).WithError(NameErrors.Empty)
            .MaximumLength(Name.MaxLength).WithError(NameErrors.TooLong);

        RuleFor(x => x.Description)
            .Must(description => !string.IsNullOrWhiteSpace(description)).WithError(DescriptionErrors.Empty)
            .MaximumLength(Description.MaxLength).WithError(DescriptionErrors.TooLong);

        RuleFor(x => x.PropertyType)
            .Must(type => EnumNames.TryParse<PropertyType>(type, out _))
            .WithError(ValidationErrors.Listing.InvalidPropertyType);

        RuleFor(x => x.Address)
            .Must(IsValidAddress)
            .WithError(ValidationErrors.Listing.InvalidAddress);

        RuleFor(x => x)
            .Must(x => x.Latitude is >= -90 and <= 90 && x.Longitude is >= -180 and <= 180)
            .WithError(ApartmentErrors.InvalidCoordinates);

        RuleFor(x => x.Currency)
            .Must(ListingMapper.IsSupportedCurrency)
            .WithError(ValidationErrors.Listing.InvalidCurrency);

        RuleFor(x => x)
            .Must(x => x.Price is > 0 and <= MaxPrice &&
                       x.CleaningFee is >= 0 and <= MaxPrice &&
                       x.WeekendPrice is null or (> 0 and <= MaxPrice))
            .WithError(ApartmentErrors.InvalidPrice);

        RuleFor(x => x)
            .Must(x => x.WeeklyDiscountPercent is >= 0 and <= Apartment.MaxDiscountPercent &&
                       x.MonthlyDiscountPercent is >= 0 and <= Apartment.MaxDiscountPercent)
            .WithError(ApartmentErrors.InvalidDiscount);

        RuleFor(x => x)
            .Must(x => x.Bedrooms is >= 0 and <= Apartment.MaxBedrooms &&
                       x.Beds is >= 1 and <= Apartment.MaxBeds &&
                       x.Bathrooms is >= 0 and <= Apartment.MaxBathrooms &&
                       x.Bathrooms % 0.5m == 0 &&
                       x.MaximumGuestCount is >= 1 and <= Apartment.MaxGuests)
            .WithError(ApartmentErrors.InvalidCapacity);

        RuleFor(x => x.Amenities)
            .Must(amenities => amenities is not null && amenities.All(name => EnumNames.TryParse<Amenity>(name, out _)))
            .WithError(ValidationErrors.Listing.InvalidAmenity);

        RuleFor(x => x)
            .Must(x => ListingMapper.TryParseTime(x.CheckInTime, out _) && ListingMapper.TryParseTime(x.CheckOutTime, out _))
            .WithError(ValidationErrors.Listing.InvalidTime);

        RuleFor(x => x.HouseRules)
            .MaximumLength(Apartment.TextMaxLength)
            .WithError(ApartmentErrors.HouseRulesTooLong);

        RuleFor(x => x.CheckInInstructions)
            .MaximumLength(Apartment.TextMaxLength)
            .WithError(ApartmentErrors.CheckInInstructionsTooLong);

        RuleFor(x => x)
            .Must(x => x.MinNights >= 1 && x.MaxNights <= Apartment.MaxNightsLimit && x.MinNights <= x.MaxNights)
            .WithError(ApartmentErrors.InvalidStayLimits);

        RuleFor(x => x.ImageIds)
            .Must(ids => ids is not null && ids.Count is >= 1 and <= Apartment.MaxImages)
            .WithError(ApartmentErrors.InvalidImageCount)
            .Must(ids => ids is null || ids.Distinct().Count() == ids.Count)
            .WithError(ApartmentErrors.DuplicateImage);
    }

    private static bool IsValidAddress(ListingAddressRequest? address)
    {
        if (address is null ||
            string.IsNullOrWhiteSpace(address.Country) ||
            string.IsNullOrWhiteSpace(address.City) ||
            string.IsNullOrWhiteSpace(address.Street) ||
            address.State is null ||
            address.ZipCode is null)
        {
            return false;
        }

        string[] parts = [address.Country, address.State, address.ZipCode, address.City, address.Street];

        return parts.All(part => part.Length <= AddressPartMaxLength);
    }
}
