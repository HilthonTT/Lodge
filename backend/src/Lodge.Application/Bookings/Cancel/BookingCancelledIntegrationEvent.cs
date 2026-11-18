using Lodge.Application.Abstractions.Messaging;
using Lodge.Domain.Bookings.Events;
using Newtonsoft.Json;

namespace Lodge.Application.Bookings.Cancel;

/// <summary>
/// Represents the integration event that is raised when a booking is completed.
/// </summary>
public sealed class BookingCancelledIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BookingCancelledIntegrationEvent"/> class.
    /// </summary>
    internal BookingCancelledIntegrationEvent(BookingCancelledDomainEvent bookingCancelledDomainEvent)
    {
        BookingId = bookingCancelledDomainEvent.BookingId;
    }

    [JsonConstructor]
    private BookingCancelledIntegrationEvent(Guid bookingId) => BookingId = bookingId;

    /// <summary>
    /// Gets the booking identifier.
    /// </summary>
    public Guid BookingId { get; }
}
