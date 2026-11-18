namespace Lodge.Contracts.Apartments;

/// <summary>
/// A listing as its host sees it: the public fields plus the private and management ones.
/// </summary>
public sealed class HostListingResponse : ApartmentResponse
{
    public string? CheckInInstructions { get; init; }

    public bool IsListed { get; init; }

    public DateTime CreatedOnUtc { get; init; }

    public DateTime? ModifiedOnUtc { get; init; }
}
