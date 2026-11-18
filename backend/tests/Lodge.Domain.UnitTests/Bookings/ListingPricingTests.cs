using Lodge.Domain.Apartments;
using Lodge.Domain.Bookings;
using Lodge.Domain.Core.Primitives;

namespace Lodge.Domain.UnitTests.Bookings;

public class ListingPricingTests
{
    // 2026-07-01 is a Wednesday: Friday 3rd and Saturday 4th are weekend nights.
    private static readonly DateOnly Wednesday = new(2026, 7, 1);

    private readonly PricingService _pricingService = new();

    [Fact]
    public void CalculatePrice_Should_UseTheWeekendPrice_ForFridayAndSaturdayNights()
    {
        Apartment apartment = TestData.CreateApartment(TestData.Details(price: 100) with { WeekendPrice = 150 });
        DateRange stay = DateRange.Create(Wednesday, Wednesday.AddDays(5)).Value; // Wed, Thu, Fri, Sat, Sun nights

        PricingDetails details = _pricingService.CalculatePrice(apartment, stay);

        details.Nights.Should().Be(5);
        details.PriceForPeriod.Amount.Should().Be(100 + 100 + 150 + 150 + 100);
        details.AverageNightlyPrice.Amount.Should().Be(120);
        details.DiscountType.Should().Be(StayDiscountType.None);
        details.Discount.Amount.Should().Be(0);
    }

    [Fact]
    public void CalculatePrice_Should_PreferTheHostsOverride_OverWeekendAndBasePrice()
    {
        Apartment apartment = TestData.CreateApartment(TestData.Details(price: 100) with { WeekendPrice = 150 });
        DateRange stay = DateRange.Create(Wednesday, Wednesday.AddDays(4)).Value;
        var overrides = new Dictionary<DateOnly, decimal>
        {
            [Wednesday] = 80,
            [Wednesday.AddDays(2)] = 300, // Friday
        };

        PricingDetails details = _pricingService.CalculatePrice(apartment, stay, overrides);

        details.PriceForPeriod.Amount.Should().Be(80 + 100 + 300 + 150);
    }

    [Fact]
    public void CalculatePrice_Should_ApplyTheWeeklyDiscount_FromSevenNights()
    {
        Apartment apartment = TestData.CreateApartment(
            TestData.Details(price: 100, cleaningFee: 50) with { WeeklyDiscountPercent = 10, MonthlyDiscountPercent = 30 });

        PricingDetails sixNights = _pricingService.CalculatePrice(apartment, DateRange.Create(Wednesday, Wednesday.AddDays(6)).Value);
        PricingDetails sevenNights = _pricingService.CalculatePrice(apartment, DateRange.Create(Wednesday, Wednesday.AddDays(7)).Value);

        sixNights.DiscountType.Should().Be(StayDiscountType.None);
        sixNights.TotalPrice.Amount.Should().Be(650);

        sevenNights.DiscountType.Should().Be(StayDiscountType.Weekly);
        sevenNights.PriceForPeriod.Amount.Should().Be(700);
        sevenNights.Discount.Amount.Should().Be(70);
        sevenNights.TotalPrice.Amount.Should().Be(700 - 70 + 50);
    }

    [Fact]
    public void CalculatePrice_Should_ApplyTheMonthlyDiscount_FromTwentyEightNights()
    {
        Apartment apartment = TestData.CreateApartment(
            TestData.Details(price: 100) with { WeeklyDiscountPercent = 10, MonthlyDiscountPercent = 25 });

        PricingDetails details = _pricingService.CalculatePrice(apartment, DateRange.Create(Wednesday, Wednesday.AddDays(28)).Value);

        details.DiscountType.Should().Be(StayDiscountType.Monthly);
        details.Discount.Amount.Should().Be(700);
        details.TotalPrice.Amount.Should().Be(2100);
    }

    [Fact]
    public void CalculatePrice_Should_FallBackToTheWeeklyDiscount_WhenThereIsNoMonthlyDiscount()
    {
        Apartment apartment = TestData.CreateApartment(TestData.Details(price: 100) with { WeeklyDiscountPercent = 10 });

        PricingDetails details = _pricingService.CalculatePrice(apartment, DateRange.Create(Wednesday, Wednesday.AddDays(30)).Value);

        details.DiscountType.Should().Be(StayDiscountType.Weekly);
        details.Discount.Amount.Should().Be(300);
    }

    [Fact]
    public void CalculatePrice_Should_ChargeTheAmenityUpCharge_OnTheDiscountedNights()
    {
        // WiFi = 2%.
        Apartment apartment = TestData.CreateApartment(
            TestData.Details(price: 100, cleaningFee: 0, Amenity.WiFi) with { WeeklyDiscountPercent = 50 });

        PricingDetails details = _pricingService.CalculatePrice(apartment, DateRange.Create(Wednesday, Wednesday.AddDays(7)).Value);

        details.Discount.Amount.Should().Be(350);
        details.AmenitiesUpCharge.Amount.Should().Be(7);
        details.TotalPrice.Amount.Should().Be(357);
    }

    [Theory]
    [InlineData(1, false, false)]
    [InlineData(2, true, false)]
    [InlineData(14, true, false)]
    [InlineData(15, true, true)]
    public void EnsureStayLength_Should_EnforceMinAndMaxNights(int nights, bool longEnough, bool tooLong)
    {
        Apartment apartment = TestData.CreateApartment(TestData.Details() with { MinNights = 2, MaxNights = 14 });

        Result result = apartment.EnsureStayLength(nights);

        if (!longEnough)
        {
            result.Error.Code.Should().Be("Booking.StayTooShort");
        }
        else if (tooLong)
        {
            result.Error.Code.Should().Be("Booking.StayTooLong");
        }
        else
        {
            result.IsSuccess.Should().BeTrue();
        }
    }

    [Fact]
    public void Create_Should_RejectInvalidStayLimitsAndDiscounts()
    {
        Apartment.Create(TestData.OwnerId, TestData.Details() with { MinNights = 5, MaxNights = 4 }, [ApartmentImage.FromPath("/a.png")])
            .Error.Should().Be(ApartmentErrors.InvalidStayLimits);

        Apartment.Create(TestData.OwnerId, TestData.Details() with { WeeklyDiscountPercent = 91 }, [ApartmentImage.FromPath("/a.png")])
            .Error.Should().Be(ApartmentErrors.InvalidDiscount);

        Apartment.Create(TestData.OwnerId, TestData.Details() with { Bathrooms = 1.25m }, [ApartmentImage.FromPath("/a.png")])
            .Error.Should().Be(ApartmentErrors.InvalidCapacity);

        Apartment.Create(TestData.OwnerId, TestData.Details(), [])
            .Error.Should().Be(ApartmentErrors.InvalidImageCount);
    }

    [Fact]
    public void SetImages_Should_KeepExistingPhotos_ReorderThem_AndRemoveMissingOnes()
    {
        ApartmentImage first = ApartmentImage.FromPath("/images/one.png");
        ApartmentImage second = ApartmentImage.FromPath("/images/two.png");
        ApartmentImage third = ApartmentImage.FromPath("/images/three.png");
        Apartment apartment = Apartment.Create(TestData.OwnerId, TestData.Details(), [first, second, third]).Value;
        ApartmentImage uploaded = ApartmentImage.FromFile(Guid.NewGuid());

        apartment.SetImages([third, uploaded, first]).IsSuccess.Should().BeTrue();

        apartment.OrderedImages().Select(image => image.Id).Should().Equal(third.Id, uploaded.Id, first.Id);
        apartment.OrderedImages()[0].Path.Should().Be("/images/three.png");
        apartment.HasImage(second.Id).Should().BeFalse();
        apartment.SetImages([first, first]).Error.Should().Be(ApartmentErrors.DuplicateImage);
    }
}
