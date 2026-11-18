using Lodge.Application.Abstractions.Messaging;

namespace Lodge.Application.Hosting.Bookings;

/// <summary>
/// The host cancels a confirmed booking before check-in (<c>PUT /hosting/bookings/{id}/cancel</c>): the guest gets a
/// full refund, the payout is cancelled and the cancellation counts in the host's Superhost cancellation rate.
/// </summary>
public sealed record HostCancelBookingCommand(Guid BookingId) : ICommand;
