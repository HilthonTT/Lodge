using Lodge.Domain.Apartments;
using Lodge.Domain.Bookings;

namespace Lodge.Domain.UnitTests.Bookings;

public class PricingServiceTests
{
    private static readonly DateOnly Start = new(2026, 7, 1);

    [Fact]
    public void CalculatePrice_Should_ChargePerNight_PlusCleaningFee()
    {
        var apartment = TestData.CreateApartment(price: 100, cleaningFee: 50);
        DateRange period = DateRange.Create(Start, Start.AddDays(3)).Value;

        PricingDetails details = new PricingService().CalculatePrice(apartment, period);

        details.PriceForPeriod.Amount.Should().Be(300);
        details.CleaningFee.Amount.Should().Be(50);
        details.AmenitiesUpCharge.Amount.Should().Be(0);
        details.TotalPrice.Amount.Should().Be(350);
    }

    [Fact]
    public void CalculatePrice_Should_AddAmenityUpCharges()
    {
        // WiFi = 2%, Spa = 6% -> 8% of the price for the period.
        var apartment = TestData.CreateApartment(100, 0, Amenity.WiFi, Amenity.Spa);
        DateRange period = DateRange.Create(Start, Start.AddDays(2)).Value;

        PricingDetails details = new PricingService().CalculatePrice(apartment, period);

        details.AmenitiesUpCharge.Amount.Should().Be(16);
        details.TotalPrice.Amount.Should().Be(216);
        details.TotalPrice.Currency.Code.Should().Be("USD");
    }
}
