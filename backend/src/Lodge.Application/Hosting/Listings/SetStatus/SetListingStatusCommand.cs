using Lodge.Application.Abstractions.Messaging;

namespace Lodge.Application.Hosting.Listings.SetStatus;

/// <summary>
/// Lists or unlists a listing (owner or admin). Unlisted listings are hidden from guests, but existing bookings stay.
/// </summary>
public sealed record SetListingStatusCommand(Guid ApartmentId, bool IsListed) : ICommand;
