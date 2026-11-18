namespace Lodge.Contracts.Apartments;

/// <summary>
/// Represents the address response.
/// </summary>
public sealed class AddressResponse
{
    /// <summary>
    /// Gets the country.
    /// </summary>
    public string Country { get; set; } = string.Empty;

    /// <summary>
    /// Gets the state.
    /// </summary>
    public string State { get; set; } = string.Empty;

    /// <summary>
    /// Gets the city.
    /// </summary>
    public string City { get; set; } = string.Empty;

    /// <summary>
    /// Gets the street.
    /// </summary>
    public string Street { get; set; } = string.Empty;
}