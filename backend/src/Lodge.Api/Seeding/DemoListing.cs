using Lodge.Domain.Apartments;
using Lodge.Domain.Shared;

namespace Lodge.Api.Seeding;

/// <summary>
/// A demo listing; one per image in <c>wwwroot/images</c>.
/// </summary>
internal sealed record DemoListing(
    string Name,
    string Description,
    PropertyType PropertyType,
    Address Address,
    double Latitude,
    double Longitude,
    decimal Price,
    decimal CleaningFee,
    int Bedrooms,
    int Beds,
    decimal Bathrooms,
    int Guests,
    string ImagePath,
    Amenity[] Amenities,
    DemoHost Host)
{
    private static readonly Currency Usd = Currency.FromCode("USD");

    public bool InstantBook { get; init; } = true;

    public CancellationPolicy CancellationPolicy { get; init; } = CancellationPolicy.Moderate;

    public int MinNights { get; init; } = 1;

    public decimal? WeekendPrice { get; init; }

    public int WeeklyDiscountPercent { get; init; }

    public int MonthlyDiscountPercent { get; init; }

    public bool PetsAllowed { get; init; }

    public string? HouseRules { get; init; }

    public string CheckInInstructions { get; init; } =
        "The key is in the lockbox next to the front door; the code is sent the day before arrival.";

    public ListingDetails ToDetails() => new()
    {
        Name = Domain.Apartments.Name.Create(Name).Value,
        Description = Domain.Apartments.Description.Create(Description).Value,
        PropertyType = PropertyType,
        Address = Address,
        Latitude = Latitude,
        Longitude = Longitude,
        Price = new Money(Price, Usd),
        CleaningFee = new Money(CleaningFee, Usd),
        WeekendPrice = WeekendPrice,
        WeeklyDiscountPercent = WeeklyDiscountPercent,
        MonthlyDiscountPercent = MonthlyDiscountPercent,
        Bedrooms = Bedrooms,
        Beds = Beds,
        Bathrooms = Bathrooms,
        MaximumGuestCount = Guests,
        Amenities = Amenities,
        CheckInTime = new TimeOnly(15, 0),
        CheckOutTime = new TimeOnly(11, 0),
        PetsAllowed = PetsAllowed,
        SmokingAllowed = false,
        PartiesAllowed = false,
        HouseRules = HouseRules ?? "Quiet hours from 22:00 to 08:00. Please take off your shoes inside.",
        CheckInInstructions = CheckInInstructions,
        InstantBook = InstantBook,
        CancellationPolicy = CancellationPolicy,
        MinNights = MinNights,
        MaxNights = Apartment.MaxNightsLimit,
    };
}
