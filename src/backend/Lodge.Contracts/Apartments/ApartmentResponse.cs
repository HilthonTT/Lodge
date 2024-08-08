namespace Lodge.Contracts.Apartments;

/// <summary>
/// Represents the apartment response.
/// </summary>
public sealed class ApartmentResponse
{
    /// <summary>
    /// Initializes a new instance of <see cref="ApartmentResponse"/> class.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <param name="name">The name.</param>
    /// <param name="description">The description.</param>
    /// <param name="price">The price.</param>
    /// <param name="currency">The currency.</param>
    /// <param name="imageUrl">The image url.</param>
    /// <param name="address">The address.</param>
    public ApartmentResponse(
        Guid id,
        string name,
        string description,
        decimal price,
        string currency,
        string imageUrl,
        AddressResponse address)
    {
        Id = id;
        Name = name;
        Description = description;
        Price = price;
        Currency = currency;
        ImageUrl = imageUrl;
        Address = address;
    }

    public ApartmentResponse()
    {
    }

    /// <summary>
    /// Gets the identifier.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Gets the name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the description.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the price.
    /// </summary>
    public decimal Price { get; }

    /// <summary>
    /// Gets the currency.
    /// </summary>
    public string Currency { get; }

    /// <summary>
    /// Gets the image url.
    /// </summary>
    public string ImageUrl { get; }

    /// <summary>
    /// Gets and sets the address.
    /// </summary>
    public AddressResponse Address { get; set; }
}