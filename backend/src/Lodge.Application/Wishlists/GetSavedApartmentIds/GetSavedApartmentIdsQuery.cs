using Lodge.Application.Abstractions.Messaging;

namespace Lodge.Application.Wishlists.GetSavedApartmentIds;

/// <summary>
/// Gets the ids of every apartment the current user saved in any wishlist (for the heart icons).
/// </summary>
public sealed record GetSavedApartmentIdsQuery : IQuery<List<Guid>>;
