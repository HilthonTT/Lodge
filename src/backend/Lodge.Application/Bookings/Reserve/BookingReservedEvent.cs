﻿using MediatR;

namespace Lodge.Application.Bookings.Reserve;

/// <summary>
/// Represents the event that is triggered when a booking is reserved.
/// </summary>
/// <param name="BookingId">The booking identifier.</param>
internal sealed record BookingReservedEvent(Guid BookingId) : INotification;