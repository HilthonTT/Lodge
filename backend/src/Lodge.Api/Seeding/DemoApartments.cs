using Lodge.Domain.Apartments;

namespace Lodge.Api.Seeding;

/// <summary>
/// The demo listings: Sam hosts eight of them (three need his approval), Priya two. Cancellation policies vary:
/// Flexible (cottage, houseboat, treehouse), Moderate (cabin, family home, château, lake house) and Strict (seaside
/// retreat, igloo, villa).
/// </summary>
internal static class DemoApartments
{
    public const string PineRidgeCabin = "Pine Ridge Cabin";
    public const string LavenderHillCottage = "Lavender Hill Cottage";
    public const string MapleGroveFamilyHome = "Maple Grove Family Home";
    public const string SeasideFamilyRetreat = "Seaside Family Retreat";
    public const string CanalHouseboat = "Canal Houseboat";
    public const string AuroraGlassIgloo = "Aurora Glass Igloo";
    public const string HillsideLuxuryVilla = "Hillside Luxury Villa";
    public const string ChateauDesVignes = "Château des Vignes";
    public const string MinimalistLakeHouse = "Minimalist Lake House";
    public const string RedwoodTreehouse = "Redwood Treehouse";

    public static readonly IReadOnlyList<DemoListing> All =
    [
        new(
            PineRidgeCabin,
            "A hand-built log cabin tucked between pine trees, with a wood stove, a private hot tub and sweeping views of the Rockies. Ski lifts are a 10-minute drive away.",
            PropertyType.Cabin,
            new Address("United States", "Colorado", "81611", "Aspen", "112 Pine Ridge Rd"),
            39.1911, -106.8175,
            Price: 185, CleaningFee: 60, Bedrooms: 2, Beds: 3, Bathrooms: 1.5m, Guests: 4, "/images/cabin.png",
            [Amenity.WiFi, Amenity.Parking, Amenity.MountainView, Amenity.PetFriendly, Amenity.Cabins, Amenity.HotTub, Amenity.Fireplace, Amenity.Kitchen, Amenity.Heating],
            DemoHost.Sam)
        {
            MinNights = 2,
            WeekendPrice = 225,
            WeeklyDiscountPercent = 10,
            PetsAllowed = true,
        },
        new(
            LavenderHillCottage,
            "A 17th-century stone cottage in one of England's prettiest villages. Exposed beams, a walled lavender garden and a pub around the corner.",
            PropertyType.Cottage,
            new Address("United Kingdom", "Gloucestershire", "GL7 5NP", "Bibury", "3 Arlington Row"),
            51.7590, -1.8335,
            Price: 140, CleaningFee: 45, Bedrooms: 2, Beds: 2, Bathrooms: 1, Guests: 3, "/images/cottage.png",
            [Amenity.WiFi, Amenity.GardenView, Amenity.PetFriendly, Amenity.Kitchen, Amenity.Fireplace, Amenity.Heating],
            DemoHost.Sam)
        {
            CancellationPolicy = CancellationPolicy.Flexible,
            InstantBook = false,
            MinNights = 2,
            PetsAllowed = true,
        },
        new(
            MapleGroveFamilyHome,
            "A bright craftsman house with a large backyard, a fully equipped kitchen and bikes for the whole family. Close to parks and food carts.",
            PropertyType.House,
            new Address("United States", "Oregon", "97214", "Portland", "2245 SE Maple St"),
            45.5059, -122.6402,
            Price: 220, CleaningFee: 80, Bedrooms: 4, Beds: 5, Bathrooms: 2.5m, Guests: 8, "/images/family-house.png",
            [Amenity.WiFi, Amenity.Parking, Amenity.AirConditioning, Amenity.GardenView, Amenity.Terrace, Amenity.Kitchen, Amenity.Washer, Amenity.Workspace, Amenity.EVCharger],
            DemoHost.Sam)
        {
            MinNights = 2,
            WeeklyDiscountPercent = 8,
            MonthlyDiscountPercent = 20,
        },
        new(
            SeasideFamilyRetreat,
            "A whitewashed villa five minutes' walk from the beach, with a saltwater pool, a shaded terrace and a barbecue for long summer evenings.",
            PropertyType.Villa,
            new Address("Portugal", "Lisbon", "2750-642", "Cascais", "Rua das Flores 18"),
            38.6979, -9.4215,
            Price: 260, CleaningFee: 90, Bedrooms: 3, Beds: 4, Bathrooms: 2, Guests: 6, "/images/family-house-2.png",
            [Amenity.WiFi, Amenity.SwimmingPool, Amenity.Terrace, Amenity.AirConditioning, Amenity.Tropical, Amenity.BeachAccess, Amenity.Kitchen, Amenity.Washer],
            DemoHost.Sam)
        {
            CancellationPolicy = CancellationPolicy.Strict,
            MinNights = 3,
            WeekendPrice = 295,
        },
        new(
            CanalHouseboat,
            "Sleep on the water in a renovated houseboat on a quiet canal in the Jordaan, with a sun deck and the city's best cafés at your doorstep.",
            PropertyType.Houseboat,
            new Address("Netherlands", "North Holland", "1015 DT", "Amsterdam", "Prinsengracht 263"),
            52.3752, 4.8840,
            Price: 165, CleaningFee: 40, Bedrooms: 1, Beds: 1, Bathrooms: 1, Guests: 2, "/images/house-boat.png",
            [Amenity.WiFi, Amenity.Boats, Amenity.TopCities, Amenity.Heating, Amenity.SelfCheckIn],
            DemoHost.Priya)
        {
            CancellationPolicy = CancellationPolicy.Flexible,
        },
        new(
            AuroraGlassIgloo,
            "A heated glass igloo deep in the Arctic forest. Watch the northern lights from your bed and end the day in the private sauna.",
            PropertyType.Igloo,
            new Address("Finland", "Lapland", "96930", "Rovaniemi", "Arctic Circle Way 7"),
            66.5039, 25.7294,
            Price: 310, CleaningFee: 50, Bedrooms: 1, Beds: 1, Bathrooms: 1, Guests: 2, "/images/igloo.jpg",
            [Amenity.WiFi, Amenity.Domes, Amenity.Spa, Amenity.Heating],
            DemoHost.Sam)
        {
            CancellationPolicy = CancellationPolicy.Strict,
            InstantBook = false,
            MinNights = 2,
        },
        new(
            HillsideLuxuryVilla,
            "A contemporary villa above the Pacific with an infinity pool, a home gym, a spa room and floor-to-ceiling windows in every bedroom.",
            PropertyType.Villa,
            new Address("United States", "California", "90265", "Malibu", "28500 Cliffside Dr"),
            34.0259, -118.7798,
            Price: 680, CleaningFee: 150, Bedrooms: 5, Beds: 6, Bathrooms: 5.5m, Guests: 10, "/images/luxury-house.png",
            [Amenity.WiFi, Amenity.Parking, Amenity.AirConditioning, Amenity.SwimmingPool, Amenity.Gym, Amenity.Spa, Amenity.Terrace, Amenity.Mansions, Amenity.HotTub, Amenity.Kitchen, Amenity.Washer, Amenity.Workspace, Amenity.EVCharger],
            DemoHost.Sam)
        {
            CancellationPolicy = CancellationPolicy.Strict,
            MinNights = 3,
            WeeklyDiscountPercent = 10,
            MonthlyDiscountPercent = 25,
        },
        new(
            ChateauDesVignes,
            "A restored 19th-century château surrounded by its own vineyards. Wine tastings, a heated pool and room for the whole family.",
            PropertyType.Villa,
            new Address("France", "Nouvelle-Aquitaine", "33250", "Pauillac", "Route des Châteaux 4"),
            45.1993, -0.7486,
            Price: 950, CleaningFee: 200, Bedrooms: 8, Beds: 10, Bathrooms: 6, Guests: 16, "/images/mansion.png",
            [Amenity.WiFi, Amenity.Parking, Amenity.SwimmingPool, Amenity.GardenView, Amenity.Castles, Amenity.Farms, Amenity.Kitchen, Amenity.Fireplace],
            DemoHost.Sam)
        {
            InstantBook = false,
            MinNights = 3,
        },
        new(
            MinimalistLakeHouse,
            "An architect-designed retreat on the shore of Lake Wakatipu: concrete, glass and timber, a private jetty and mountains all around.",
            PropertyType.House,
            new Address("New Zealand", "Otago", "9300", "Queenstown", "41 Lakeshore Terrace"),
            -45.0312, 168.6626,
            Price: 340, CleaningFee: 85, Bedrooms: 3, Beds: 3, Bathrooms: 2, Guests: 6, "/images/modern-house.png",
            [Amenity.WiFi, Amenity.AirConditioning, Amenity.Terrace, Amenity.MountainView, Amenity.Lakefront, Amenity.Kitchen, Amenity.Workspace, Amenity.SelfCheckIn],
            DemoHost.Priya)
        {
            MinNights = 2,
        },
        new(
            RedwoodTreehouse,
            "A cosy treehouse six metres up in an old-growth redwood grove, reached by a rope bridge. Off the grid, but with hot showers.",
            PropertyType.Treehouse,
            new Address("United States", "California", "95446", "Guerneville", "18 Redwood Hollow"),
            38.5021, -122.9961,
            Price: 210, CleaningFee: 55, Bedrooms: 1, Beds: 2, Bathrooms: 1, Guests: 3, "/images/tree-house.png",
            [Amenity.WiFi, Amenity.PetFriendly, Amenity.Treehouses, Amenity.Heating],
            DemoHost.Sam)
        {
            CancellationPolicy = CancellationPolicy.Flexible,
            WeekendPrice = 245,
            PetsAllowed = true,
        },
    ];
}
