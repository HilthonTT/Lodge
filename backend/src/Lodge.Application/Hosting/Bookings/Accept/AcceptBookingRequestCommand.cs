using Lodge.Application.Abstractions.Messaging;

namespace Lodge.Application.Hosting.Bookings.Accept;

/// <summary>
/// The host accepts a booking request: PendingApproval → Reserved (the guest can then pay).
/// </summary>
public sealed record AcceptBookingRequestCommand(Guid BookingId) : ICommand;
