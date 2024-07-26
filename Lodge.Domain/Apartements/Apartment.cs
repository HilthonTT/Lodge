using Lodge.Domain.Apartements.Events;
using Lodge.Domain.Core.Abstractions;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Shared;

namespace Lodge.Domain.Apartements;

/// <summary>
/// Represents the apartment entity.
/// </summary>
public sealed class Apartment : Entity, IAuditableEntity
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Apartment"/> class.
    /// </summary>
    /// <param name="id">The apartment's id.</param>
    /// <param name="name">The apartment's name.</param>
    /// <param name="description">The apartment's description.</param>
    /// <param name="address">The apartment's address.</param>
    /// <param name="price">The apartment's price.</param>
    /// <param name="cleaningFee">The apartment's cleaning fee.</param>
    /// <param name="imageId">The apartment's image id.</param>
    /// <param name="amenities">The apartment's amenities.</param>
    private Apartment(
        Guid id,
        Name name,
        Description description,
        Address address,
        Money price,
        Money cleaningFee,
        Guid imageId,
        List<Amenity> amenities)
        : base(id)
    {
        Name = name;
        Description = description;
        Address = address;
        Price = price;
        CleaningFee = cleaningFee;
        ImageId = imageId;
        Amenities = amenities;
    }

    /// <summary>
    /// Initializes a new blank instance of the <see cref="Apartment"/> class.
    /// </summary>
    /// <remarks>
    /// Required by EF Core.
    /// </remarks>
    private Apartment() 
    {
    }

    public Name Name { get; private set; }

    public Description Description { get; private set; }

    public Address Address { get; private set; }

    public Money Price { get; private set; }

    public Money CleaningFee { get; private set; }

    public Guid ImageId { get; private set; }

    public DateTime? LastBookedOnUtc  { get; internal set; }

    public List<Amenity> Amenities { get; private set; } = [];

    public DateTime CreatedOnUtc { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }

    /// <summary>
    /// Creates a new apartment.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="description">The description.</param>
    /// <param name="address">The address.</param>
    /// <param name="price">The price.</param>
    /// <param name="cleaningFee">The cleaning fee.</param>
    /// <param name="imageId">The apartment's image id.</param>
    /// <param name="amenities">The amenities.</param>
    /// <returns>The newly created apartment instance.</returns>
    public static Apartment Create(
        Name name, 
        Description description, 
        Address address, 
        Money price, 
        Money cleaningFee, 
        Guid imageId,
        List<Amenity> amenities)
    {
        var apartment = new Apartment(
            Guid.NewGuid(),
            name, 
            description,
            address,
            price,
            cleaningFee,
            imageId,
            amenities);

        apartment.RaiseDomainEvent(new ApartmentCreatedDomainEvent(apartment.Id));

        return apartment;
    }
}
