using FluentValidation.Results;
using Lodge.Application.Abstractions.Assets;
using Lodge.Application.Apartments.Search;
using Lodge.Application.Bookings;
using Lodge.Application.Hosting.Calendar;
using Lodge.Application.Hosting.Calendar.Update;
using Lodge.Application.Hosting.Listings;
using Lodge.Contracts.Bookings;
using Lodge.Contracts.Hosting;
using Lodge.Domain.Bookings;
using NSubstitute;

namespace Lodge.Application.UnitTests.Hosting;

public class HostingValidationTests
{
    private static readonly ListingRequest ValidListing = new()
    {
        Name = "Harbour loft",
        Description = "A bright loft above the harbour.",
        PropertyType = "apartment",
        Address = new ListingAddressRequest
        {
            Country = "Norway",
            State = "Vestland",
            ZipCode = "5003",
            City = "Bergen",
            Street = "Bryggen 1",
        },
        Latitude = 60.3971,
        Longitude = 5.3240,
        Price = 150,
        Currency = "EUR",
        CleaningFee = 30,
        Bedrooms = 1,
        Beds = 2,
        Bathrooms = 1.5m,
        MaximumGuestCount = 3,
        Amenities = ["WiFi", "kitchen"],
        CheckInTime = "16:00",
        CheckOutTime = "10:30",
        MinNights = 2,
        MaxNights = 30,
        ImageIds = [Guid.NewGuid()],
    };

    private readonly ListingRequestValidator _listingValidator = new();

    [Fact]
    public void ListingRequestValidator_Should_AcceptAValidListing_AndMapIt()
    {
        _listingValidator.Validate(ValidListing).IsValid.Should().BeTrue();

        var details = ListingMapper.ToDetails(ValidListing).Value;

        details.PropertyType.Should().Be(Domain.Apartments.PropertyType.Apartment);
        details.Amenities.Should().Equal(Domain.Apartments.Amenity.WiFi, Domain.Apartments.Amenity.Kitchen);
        details.CheckOutTime.Should().Be(new TimeOnly(10, 30));
        details.Price.Currency.Code.Should().Be("EUR");
    }

    [Theory]
    [InlineData("Listing.InvalidPropertyType")]
    [InlineData("Listing.InvalidAmenity")]
    [InlineData("Listing.InvalidTime")]
    [InlineData("Apartment.InvalidCapacity")]
    [InlineData("Apartment.InvalidStayLimits")]
    [InlineData("Apartment.InvalidImageCount")]
    [InlineData("Listing.InvalidCurrency")]
    public void ListingRequestValidator_Should_RejectInvalidValues(string expectedCode)
    {
        ListingRequest request = expectedCode switch
        {
            "Listing.InvalidPropertyType" => ValidListing with { PropertyType = "Spaceship" },
            "Listing.InvalidAmenity" => ValidListing with { Amenities = ["WiFi", "Moat"] },
            "Listing.InvalidTime" => ValidListing with { CheckInTime = "4pm" },
            "Apartment.InvalidCapacity" => ValidListing with { Bathrooms = 1.3m },
            "Apartment.InvalidStayLimits" => ValidListing with { MinNights = 10, MaxNights = 5 },
            "Apartment.InvalidImageCount" => ValidListing with { ImageIds = [] },
            _ => ValidListing with { Currency = "GBP" },
        };

        ValidationResult result = _listingValidator.Validate(request);

        result.Errors.Select(error => error.ErrorCode).Should().Contain(expectedCode);
    }

    [Theory]
    [InlineData(null, "2026-12", "SearchApartments.MonthsRequireFlexible")]
    [InlineData("week", "2026", "SearchApartments.InvalidMonths")]
    [InlineData("week", "2026-13", "SearchApartments.InvalidMonths")]
    [InlineData("fortnight", "2026-12", "SearchApartments.InvalidFlexible")]
    public void SearchValidator_Should_ValidateFlexibleDates(string? flexible, string? month, string expectedCode)
    {
        var query = new SearchApartmentQuery { Flexible = flexible, Months = month is null ? [] : [month] };

        ValidationResult result = new SearchApartmentQueryValidator().Validate(query);

        result.Errors.Select(error => error.ErrorCode).Should().Contain(expectedCode);
    }

    [Fact]
    public void SearchValidator_Should_AcceptFlexibleDatesWithoutMonths()
    {
        var query = new SearchApartmentQuery { Flexible = "week" };

        new SearchApartmentQueryValidator().Validate(query).IsValid.Should().BeTrue();
    }

    [Fact]
    public void RequestExpiry_Should_FollowTheExpiryJobRules()
    {
        var createdOnUtc = new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc);

        // 24 hours after the request...
        BookingQueries.RequestExpiresOnUtc(createdOnUtc, new DateOnly(2026, 6, 20), 24)
            .Should().Be(new DateTime(2026, 6, 2, 10, 0, 0, DateTimeKind.Utc));

        // ...unless the check-in day ends first.
        BookingQueries.RequestExpiresOnUtc(createdOnUtc, new DateOnly(2026, 6, 1), 24)
            .Should().Be(new DateTime(2026, 6, 2, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void SearchValidator_Should_RejectFlexibleDatesWithExactDates_AndUnknownFilters()
    {
        var query = new SearchApartmentQuery
        {
            Flexible = "weekend",
            Months = ["2026-12"],
            StartDate = new DateOnly(2026, 12, 1),
            EndDate = new DateOnly(2026, 12, 3),
            Amenities = ["Moat"],
            PropertyTypes = ["Castle"],
            MinPrice = 500,
            MaxPrice = 100,
        };

        ValidationResult result = new SearchApartmentQueryValidator().Validate(query);

        result.Errors.Select(error => error.ErrorCode).Should().Contain(
        [
            "SearchApartments.FlexibleWithExactDates",
            "SearchApartments.InvalidAmenity",
            "SearchApartments.InvalidPropertyType",
            "SearchApartments.InvalidPriceRange",
        ]);
    }

    [Fact]
    public void UpdateCalendarValidator_Should_RequireAChange_AndLimitTheRange()
    {
        var validator = new UpdateCalendarCommandValidator();
        var from = new DateOnly(2030, 1, 1);

        validator.Validate(new UpdateCalendarCommand(Guid.NewGuid(), from, from.AddDays(3), null, null, false))
            .Errors.Select(error => error.ErrorCode).Should().Contain("Calendar.NothingToChange");

        validator.Validate(new UpdateCalendarCommand(Guid.NewGuid(), from, from.AddDays(366), true, null, false))
            .Errors.Select(error => error.ErrorCode).Should().Contain("Apartment.InvalidCalendarRange");

        validator.Validate(new UpdateCalendarCommand(Guid.NewGuid(), from, from.AddDays(365), true, 120, false))
            .IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("guest", BookingStatus.Reserved, false)]
    [InlineData("guest", BookingStatus.Confirmed, true)]
    [InlineData("host", BookingStatus.PendingApproval, true)]
    [InlineData("other", BookingStatus.Confirmed, false)]
    public void Present_Should_OnlyShowCheckInInstructions_ToTheHostAndConfirmedGuests(
        string caller,
        BookingStatus status,
        bool visible)
    {
        Guid guestId = Guid.NewGuid();
        Guid hostId = Guid.NewGuid();
        var booking = new BookingResponse
        {
            UserId = guestId,
            ListingHostId = hostId,
            Status = (int)status,
            CheckInInstructions = "Code 1234",
            ApartmentImageUrl = "/images/cabin.png",
        };

        IAssetUrlResolver resolver = Substitute.For<IAssetUrlResolver>();
        resolver.Resolve(Arg.Any<string>()).Returns(call => "http://lodge.test" + call.Arg<string>());

        Guid callerId = caller switch
        {
            "guest" => guestId,
            "host" => hostId,
            _ => Guid.NewGuid(),
        };

        BookingQueries.Present(booking, callerId, resolver, DateOnly.FromDateTime(DateTime.UtcNow), 24);

        booking.CheckInInstructions.Should().Be(visible ? "Code 1234" : null);
        booking.ApartmentImageUrl.Should().Be("http://lodge.test/images/cabin.png");
    }
}
