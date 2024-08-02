namespace Lodge.Contracts.Apartments;

/// <summary>
/// Represents the address response.
/// </summary>
public sealed class AddressResponse
{
    /// <summary>
    /// Initializes a new instance of <see cref="AddressResponse"/> class.
    /// </summary>
    /// <param name="country">The country.</param>
    /// <param name="state">The state.</param>
    /// <param name="city">The city.</param>
    /// <param name="street">The street.</param>
    public AddressResponse(
        string country, 
        string state, 
        string city, 
        string street)
    {
        Country = country;
        State = state;
        City = city;
        Street = street;
    }

    public AddressResponse()
    {
    }

    /// <summary>
    /// Gets the country.
    /// </summary>
    public string Country { get; }

    /// <summary>
    /// Gets the state.
    /// </summary>
    public string State { get; }

    /// <summary>
    /// Gets the city.
    /// </summary>
    public string City { get; }

    /// <summary>
    /// Gets the street.
    /// </summary>
    public string Street { get; }
}