using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Apartments;

namespace Lodge.Application.Apartments.GetCalendar;

/// <summary>
/// Gets the availability and nightly price of every night from <paramref name="From"/> to <paramref name="To"/>
/// (inclusive, at most 366 days) of a listed apartment.
/// </summary>
public sealed record GetApartmentCalendarQuery(Guid ApartmentId, DateOnly From, DateOnly To)
    : IQuery<List<ApartmentCalendarDayResponse>>;
