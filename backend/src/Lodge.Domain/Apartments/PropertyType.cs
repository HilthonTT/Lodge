namespace Lodge.Domain.Apartments;

/// <summary>
/// The kind of property a listing is. Serialized over the wire by name (e.g. <c>"Cabin"</c>).
/// </summary>
public enum PropertyType
{
    Apartment = 1,
    House = 2,
    Cabin = 3,
    Villa = 4,
    Cottage = 5,
    Treehouse = 6,
    Houseboat = 7,
    Igloo = 8,
    Other = 9,
}
