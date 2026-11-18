namespace Lodge.Contracts.Apartments;

/// <summary>
/// Represents the address response.
/// </summary>
public sealed class AddressResponse
{
    public string Country { get; set; } = string.Empty;

    public string State { get; set; } = string.Empty;

    public string ZipCode { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string Street { get; set; } = string.Empty;
}
