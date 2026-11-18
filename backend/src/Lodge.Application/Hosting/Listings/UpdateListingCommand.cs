using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Hosting;

namespace Lodge.Application.Hosting.Listings;

/// <summary>
/// Replaces the details and photos of a listing (owner or admin). When <see cref="ListingRequest.IsListed"/> is set,
/// the listed status changes too.
/// </summary>
public sealed record UpdateListingCommand(Guid ApartmentId, ListingRequest Listing) : ICommand;
