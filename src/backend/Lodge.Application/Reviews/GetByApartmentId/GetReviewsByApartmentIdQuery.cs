using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Reviews;

namespace Lodge.Application.Reviews.GetByApartmentId;

/// <summary>
/// Represents the query for fetching the reviews of the apartment.
/// </summary>
/// <param name="ApartmentId">The apartment identifier.</param>
public sealed record GetReviewsByApartmentIdQuery(Guid ApartmentId) : IQuery<List<ReviewResponse>>;
