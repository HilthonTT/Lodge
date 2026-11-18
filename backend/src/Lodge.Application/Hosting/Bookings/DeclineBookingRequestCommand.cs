using Lodge.Application.Abstractions.Messaging;

namespace Lodge.Application.Hosting.Bookings;

/// <summary>
/// The host declines a booking request: PendingApproval → Rejected.
/// </summary>
public sealed record DeclineBookingRequestCommand(Guid BookingId) : ICommand;
