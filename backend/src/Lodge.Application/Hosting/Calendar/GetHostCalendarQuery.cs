using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Apartments;

namespace Lodge.Application.Hosting.Calendar;

/// <summary>
/// Gets the host's view of a listing's calendar from <paramref name="From"/> to <paramref name="To"/> (inclusive,
/// at most 366 days).
/// </summary>
public sealed record GetHostCalendarQuery(Guid ApartmentId, DateOnly From, DateOnly To)
    : IQuery<List<HostCalendarDayResponse>>;
