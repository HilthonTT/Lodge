using Lodge.Domain.Apartments;
using Lodge.Domain.Bookings;
using Lodge.Domain.Shared;

namespace Lodge.Application.UnitTests;

internal static class TestApartments
{
    public static Apartment Create(decimal price = 100, bool instantBook = true, Guid? ownerId = null)
    {
        Currency usd = Currency.FromCode("USD");

        var details = new ListingDetails
        {
            Name = Name.Create("Name").Value,
            Description = Description.Create("Description").Value,
            PropertyType = PropertyType.House,
            Address = new Address("Country", "State", "ZipCode", "City", "Street"),
            Latitude = 10,
            Longitude = 20,
            Price = new Money(price, usd),
            CleaningFee = Money.Zero(usd),
            Bedrooms = 2,
            Beds = 2,
            Bathrooms = 1,
            MaximumGuestCount = 5,
            InstantBook = instantBook,
        };

        return Apartment.Create(ownerId ?? Guid.NewGuid(), details, [ApartmentImage.FromPath("/images/cabin.png")]).Value;
    }

    public static Booking Reserve(Apartment apartment, DateOnly start, DateOnly end, DateTime utcNow)
    {
        DateRange duration = DateRange.Create(start, end).Value;

        return Booking.Reserve(
            apartment,
            Guid.NewGuid(),
            duration,
            2,
            null,
            utcNow,
            new PricingService().CalculatePrice(apartment, duration)).Value;
    }
}
