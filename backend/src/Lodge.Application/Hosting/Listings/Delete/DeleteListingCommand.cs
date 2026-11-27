using Lodge.Application.Abstractions.Messaging;

namespace Lodge.Application.Hosting.Listings.Delete;

/// <summary>
/// Soft-deletes a listing (owner or admin). Refused (409) while it has pending, reserved or confirmed stays that have
/// not ended. Past bookings and reviews are kept.
/// </summary>
public sealed record DeleteListingCommand(Guid ApartmentId) : ICommand;
