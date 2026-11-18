using Lodge.Application.Abstractions.Messaging;

namespace Lodge.Application.Apartments.GetDisabledDates;

/// <summary>
/// Represents the query for fetching the disabled dates of an apartment.
/// </summary>
/// <param name="ApartmentId">The apartment identifier.</param>
public sealed record GetApartmentDisabledDatesQuery(Guid ApartmentId) : IQuery<List<DateTime>>;
