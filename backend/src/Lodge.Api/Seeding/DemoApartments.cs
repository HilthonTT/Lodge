using Lodge.Domain.Apartments;
using Lodge.Domain.Shared;

namespace Lodge.Api.Seeding;

/// <summary>
/// The demo apartments, one per image in <c>wwwroot/images</c>.
/// </summary>
internal static class DemoApartments
{
    private static readonly Currency Usd = Currency.FromCode("USD");

    public static IEnumerable<Apartment> Create()
    {
        yield return Apartment(
            "Pine Ridge Cabin",
            "A hand-built log cabin tucked between pine trees, with a wood stove, a private hot tub and sweeping views of the Rockies. Ski lifts are a 10-minute drive away.",
            new Address("United States", "Colorado", "81611", "Aspen", "112 Pine Ridge Rd"),
            price: 185, cleaningFee: 60, rooms: 2, guests: 4, "/images/cabin.png",
            [Amenity.WiFi, Amenity.Parking, Amenity.MountainView, Amenity.PetFriendly, Amenity.Cabins]);

        yield return Apartment(
            "Lavender Hill Cottage",
            "A 17th-century stone cottage in one of England's prettiest villages. Exposed beams, a walled lavender garden and a pub around the corner.",
            new Address("United Kingdom", "Gloucestershire", "GL7 5NP", "Bibury", "3 Arlington Row"),
            price: 140, cleaningFee: 45, rooms: 2, guests: 3, "/images/cottage.png",
            [Amenity.WiFi, Amenity.GardenView, Amenity.CountrySide, Amenity.PetFriendly]);

        yield return Apartment(
            "Maple Grove Family Home",
            "A bright craftsman house with a large backyard, a fully equipped kitchen and bikes for the whole family. Close to parks and food carts.",
            new Address("United States", "Oregon", "97214", "Portland", "2245 SE Maple St"),
            price: 220, cleaningFee: 80, rooms: 4, guests: 8, "/images/family-house.png",
            [Amenity.WiFi, Amenity.Parking, Amenity.AirConditioning, Amenity.GardenView, Amenity.Terrace]);

        yield return Apartment(
            "Seaside Family Retreat",
            "A whitewashed villa five minutes' walk from the beach, with a saltwater pool, a shaded terrace and a barbecue for long summer evenings.",
            new Address("Portugal", "Lisbon", "2750-642", "Cascais", "Rua das Flores 18"),
            price: 260, cleaningFee: 90, rooms: 3, guests: 6, "/images/family-house-2.png",
            [Amenity.WiFi, Amenity.SwimmingPool, Amenity.Terrace, Amenity.AirConditioning, Amenity.Tropical]);

        yield return Apartment(
            "Canal Houseboat",
            "Sleep on the water in a renovated houseboat on a quiet canal in the Jordaan, with a sun deck and the city's best cafés at your doorstep.",
            new Address("Netherlands", "North Holland", "1015 DT", "Amsterdam", "Prinsengracht 263"),
            price: 165, cleaningFee: 40, rooms: 1, guests: 2, "/images/house-boat.png",
            [Amenity.WiFi, Amenity.Boats, Amenity.TopCities]);

        yield return Apartment(
            "Aurora Glass Igloo",
            "A heated glass igloo deep in the Arctic forest. Watch the northern lights from your bed and end the day in the private sauna.",
            new Address("Finland", "Lapland", "96930", "Rovaniemi", "Arctic Circle Way 7"),
            price: 310, cleaningFee: 50, rooms: 1, guests: 2, "/images/igloo.jpg",
            [Amenity.WiFi, Amenity.Domes, Amenity.Spa, Amenity.OMG]);

        yield return Apartment(
            "Hillside Luxury Villa",
            "A contemporary villa above the Pacific with an infinity pool, a home gym, a spa room and floor-to-ceiling windows in every bedroom.",
            new Address("United States", "California", "90265", "Malibu", "28500 Cliffside Dr"),
            price: 680, cleaningFee: 150, rooms: 5, guests: 10, "/images/luxury-house.png",
            [Amenity.WiFi, Amenity.Parking, Amenity.AirConditioning, Amenity.SwimmingPool, Amenity.Gym, Amenity.Spa, Amenity.Terrace, Amenity.Mansions]);

        yield return Apartment(
            "Château des Vignes",
            "A restored 19th-century château surrounded by its own vineyards. Wine tastings, a heated pool and room for the whole family.",
            new Address("France", "Nouvelle-Aquitaine", "33250", "Pauillac", "Route des Châteaux 4"),
            price: 950, cleaningFee: 200, rooms: 8, guests: 16, "/images/mansion.png",
            [Amenity.WiFi, Amenity.Parking, Amenity.SwimmingPool, Amenity.GardenView, Amenity.Castles, Amenity.Farms]);

        yield return Apartment(
            "Minimalist Lake House",
            "An architect-designed retreat on the shore of Lake Wakatipu: concrete, glass and timber, a private jetty and mountains all around.",
            new Address("New Zealand", "Otago", "9300", "Queenstown", "41 Lakeshore Terrace"),
            price: 340, cleaningFee: 85, rooms: 3, guests: 6, "/images/modern-house.png",
            [Amenity.WiFi, Amenity.AirConditioning, Amenity.Terrace, Amenity.MountainView, Amenity.Lakefront]);

        yield return Apartment(
            "Redwood Treehouse",
            "A cosy treehouse six metres up in an old-growth redwood grove, reached by a rope bridge. Off the grid, but with hot showers.",
            new Address("United States", "California", "95446", "Guerneville", "18 Redwood Hollow"),
            price: 210, cleaningFee: 55, rooms: 1, guests: 3, "/images/tree-house.png",
            [Amenity.WiFi, Amenity.PetFriendly, Amenity.CountrySide, Amenity.Treehouses]);
    }

    private static Apartment Apartment(
        string name,
        string description,
        Address address,
        decimal price,
        decimal cleaningFee,
        int rooms,
        int guests,
        string imageUrl,
        List<Amenity> amenities) =>
        Lodge.Domain.Apartments.Apartment.Create(
            Name.Create(name).Value,
            Description.Create(description).Value,
            address,
            new Money(price, Usd),
            new Money(cleaningFee, Usd),
            rooms,
            guests,
            imageUrl,
            amenities);
}
