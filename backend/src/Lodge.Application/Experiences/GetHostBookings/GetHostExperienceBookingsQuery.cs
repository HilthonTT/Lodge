using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Common;
using Lodge.Contracts.Experiences;

namespace Lodge.Application.Experiences.GetHostBookings;

/// <summary>
/// Gets one page of the bookings of the caller's experiences (optionally of one experience), latest session first.
/// </summary>
public sealed record GetHostExperienceBookingsQuery(Guid? ExperienceId, int Page, int PageSize)
    : IQuery<PagedList<ExperienceBookingResponse>>;
