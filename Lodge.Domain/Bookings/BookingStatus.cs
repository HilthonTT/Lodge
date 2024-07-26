﻿namespace Lodge.Domain.Bookings;

/// <summary>
/// Represents the booking statuses.
/// </summary>
public enum BookingStatus
{
    Reserved = 1,
    Confirmed = 2,
    Rejected = 3,
    Cancelled = 4,
    Completed = 5,
}
