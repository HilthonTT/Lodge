using Lodge.Application.Abstractions.Messaging;

namespace Lodge.Application.Bookings.ChangeDates;

/// <summary>
/// Moves the guest's pending or reserved booking to new dates: availability (ignoring the booking itself) and the
/// listing's minimum/maximum stay are checked again and the price (with the service fee and taxes) is recalculated.
/// Confirmed (paid) bookings must be
/// cancelled instead (409).
/// </summary>
public sealed record ChangeBookingDatesCommand(Guid BookingId, DateOnly StartDate, DateOnly EndDate) : ICommand;
