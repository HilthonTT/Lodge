using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Hosting;

namespace Lodge.Application.Hosting.Listings;

/// <summary>
/// Creates a listing owned by the current user (who needs a verified e-mail address).
/// </summary>
public sealed record CreateListingCommand(ListingRequest Listing) : ICommand<Guid>;
