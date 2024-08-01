using Lodge.Application.Abstractions.Messaging;
using Lodge.Domain.Bookings.Events;
using Newtonsoft.Json;

namespace Lodge.Application.Bookings.Reserve;

/// <summary>
/// Represents the integration event that is raised when a booking is reserved.
/// </summary>
public sealed class BookingReservedIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BookingReservedIntegrationEvent"/> class.
    /// </summary>
    internal BookingReservedIntegrationEvent(BookingReservedDomainEvent bookingReservedDomainEvent)
    {
        BookingId = bookingReservedDomainEvent.BookingId;
    }

    [JsonConstructor]
    private BookingReservedIntegrationEvent(Guid bookingId) => BookingId = bookingId;

    /// <summary>
    /// Gets the booking identifier.
    /// </summary>
    public Guid BookingId { get; }
}
