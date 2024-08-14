using Lodge.Application.Abstractions.Messaging;
using Lodge.Domain.Bookings.Events;
using Newtonsoft.Json;

namespace Lodge.Application.Bookings.Confirm;

/// <summary>
/// Represents the integration event that is raised when a booking is confirmed.
/// </summary>
public sealed class BookingConfirmedIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BookingConfirmedIntegrationEvent"/> class.
    /// </summary>
    internal BookingConfirmedIntegrationEvent(BookingConfirmedDomainEvent bookingConfirmedDomainEvent)
    {
        BookingId = bookingConfirmedDomainEvent.BookingId;
    }

    [JsonConstructor]
    private BookingConfirmedIntegrationEvent(Guid bookingId) => BookingId = bookingId;

    /// <summary>
    /// Gets the booking identifier.
    /// </summary>
    public Guid BookingId { get; }
}
