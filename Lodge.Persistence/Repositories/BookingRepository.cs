﻿using Lodge.Domain.Apartements;
using Lodge.Domain.Bookings;
using Lodge.Domain.Core.Primitives.Maybe;
using Microsoft.EntityFrameworkCore;

namespace Lodge.Persistence.Repositories;

/// <summary>
/// Represents the booking repository.
/// </summary>
/// <param name="context">The database context.</param>
internal sealed class BookingRepository(LodgeDbContext context) : IBookingRepository
{
    private static readonly BookingStatus[] ActiveBookingStatuses =
    {
        BookingStatus.Reserved,
        BookingStatus.Confirmed,
        BookingStatus.Completed
    };

    /// <inheritdoc />
    public async Task<Maybe<Booking?>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Bookings.FirstOrDefaultAsync(booking => booking.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> IsOverlappingAsync(Apartment apartment, DateRange duration, CancellationToken cancellationToken = default)
    {
        return await context.Bookings.AnyAsync(
            booking =>
                 booking.ApartmentId == apartment.Id &&
                    booking.Duration.Start <= duration.End &&
                    booking.Duration.End >= duration.Start &&
                    ActiveBookingStatuses.Contains(booking.Status),
                cancellationToken);
    }

    /// <inheritdoc />
    public void Add(Booking booking)
    {
        context.Bookings.Add(booking);
    }
}