﻿using MediatR;

namespace Lodge.Application.Bookings.Complete;

/// <summary>
/// Represents the event that is triggered when a booking is completed.
/// </summary>
/// <param name="BookingId">The booking identifier.</param>
internal sealed record BookingCompletedEvent(Guid BookingId) : INotification;
