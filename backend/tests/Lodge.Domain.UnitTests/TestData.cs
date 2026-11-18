using Lodge.Domain.Apartments;
using Lodge.Domain.Bookings;
using Lodge.Domain.Shared;

namespace Lodge.Domain.UnitTests;

internal static class TestData
{
    public static readonly Guid OwnerId = Guid.NewGuid();

    public static ListingDetails Details(decimal price = 100, decimal cleaningFee = 0, params Amenity[] amenities)
    {
        Currency usd = Currency.FromCode("USD");

        return new ListingDetails
        {
            Name = Name.Create("Test apartment").Value,
            Description = Description.Create("A test apartment").Value,
            PropertyType = PropertyType.Apartment,
            Address = new Address("Country", "State", "1000", "City", "Street 1"),
            Latitude = 52.37,
            Longitude = 4.89,
            Price = new Money(price, usd),
            CleaningFee = new Money(cleaningFee, usd),
            Bedrooms = 2,
            Beds = 2,
            Bathrooms = 1,
            MaximumGuestCount = 4,
            Amenities = amenities,
        };
    }

    public static Apartment CreateApartment(decimal price, decimal cleaningFee, params Amenity[] amenities) =>
        CreateApartment(Details(price, cleaningFee, amenities));

    public static Apartment CreateApartment(ListingDetails details) =>
        Apartment.Create(OwnerId, details, [ApartmentImage.FromPath("/images/cabin.png")]).Value;

    public static Booking Reserve(
        Apartment apartment,
        DateOnly start,
        int nights,
        DateTime utcNow,
        int guests = 2,
        FeeRates? fees = null)
    {
        DateRange duration = DateRange.Create(start, start.AddDays(nights)).Value;

        return Booking.Reserve(
            apartment,
            Guid.NewGuid(),
            duration,
            guests,
            null,
            utcNow,
            new PricingService().CalculatePrice(apartment, duration, feeRates: fees)).Value;
    }
}
