using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Apartments;

namespace Lodge.Application.Hosting.Listings.GetById;

/// <summary>
/// Gets a listing with its private fields (owner or admin), listed or not. Not cached.
/// </summary>
public sealed record GetHostListingQuery(Guid ApartmentId) : IQuery<HostListingResponse>;
