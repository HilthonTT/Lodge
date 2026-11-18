namespace Lodge.Domain.Apartments;

/// <summary>
/// Represents the amenities. Serialized over the wire by name (e.g. <c>"WiFi"</c>).
/// </summary>
/// <remarks>
/// The numeric values are stored in the database, so existing values never change. 11 (CountrySide),
/// 12 (TinyHomes) and 13 (OMG) were removed: they were categories, not amenities.
/// </remarks>
public enum Amenity
{
    WiFi = 1,
    AirConditioning = 2,
    Parking = 3,
    PetFriendly = 4,
    SwimmingPool = 5,
    Gym = 6,
    Spa = 7,
    Terrace = 8,
    MountainView = 9,
    GardenView = 10,
    Cabins = 14,
    Lakefront = 15,
    Treehouses = 16,
    Camping = 17,
    Castles = 18,
    Farms = 19,
    Boats = 20,
    Domes = 21,
    Tropical = 22,
    Mansions = 23,
    Caves = 24,
    Barns = 25,
    TopCities = 26,
    Kitchen = 27,
    Washer = 28,
    Heating = 29,
    Workspace = 30,
    HotTub = 31,
    Fireplace = 32,
    BeachAccess = 33,
    EVCharger = 34,
    SelfCheckIn = 35,
}
