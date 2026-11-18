using Lodge.Application.Abstractions.Messaging;

namespace Lodge.Application.Apartments.GetDisabledDates;

/// <summary>
/// Gets every unavailable night of an apartment: nights of active bookings and booking requests, and nights blocked
/// by the host.
/// </summary>
/// <param name="ApartmentId">The apartment identifier.</param>
public sealed record GetApartmentDisabledDatesQuery(Guid ApartmentId) : IQuery<List<DateTime>>;
