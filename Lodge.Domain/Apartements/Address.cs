namespace Lodge.Domain.Apartements;

/// <summary>
/// Represents the address value object.
/// </summary>
/// <param name="Country">The country name.</param>
/// <param name="State">The state name.</param>
/// <param name="ZipCode">The zip code.</param>
/// <param name="City">The city name.</param>
/// <param name="Street">The street name.</param>
public sealed record Address(
    string Country, 
    string State, 
    string ZipCode, 
    string City, 
    string Street);
