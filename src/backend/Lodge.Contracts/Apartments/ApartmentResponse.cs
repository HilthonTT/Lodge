namespace Lodge.Contracts.Apartments;

/// <summary>
/// Represents the apartment response.
/// </summary>
public sealed class ApartmentResponse
{
    /// <summary>
    /// Gets the identifier.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Gets the name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the description.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Gets the price.
    /// </summary>
    public decimal Price { get; init; }

    /// <summary>
    /// Gets the currency.
    /// </summary>
    public string Currency { get; init; } = string.Empty;

    /// <summary>
    /// Gets the image url.
    /// </summary>
    public string ImageUrl { get; init; } = string.Empty;

    /// <summary>
    /// Gets and sets the address.
    /// </summary>
    public AddressResponse Address { get; set; }
}
