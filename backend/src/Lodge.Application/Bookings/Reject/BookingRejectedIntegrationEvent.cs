using Lodge.Application.Abstractions.Messaging;
using Lodge.Domain.Bookings.Events;
using Newtonsoft.Json;

namespace Lodge.Application.Bookings.Reject;

/// <summary>
/// Represents the integration event that is raised when a booking is confirmed.
/// </summary>
public sealed class BookingRejectedIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BookingRejectedIntegrationEvent"/> class.
    /// </summary>
    internal BookingRejectedIntegrationEvent(BookingRejectedDomainEvent bookingRejectedDomainEvent)
    {
        BookingId = bookingRejectedDomainEvent.BookingId;
    }

    [JsonConstructor]
    private BookingRejectedIntegrationEvent(Guid bookingId) => BookingId = bookingId;

    /// <summary>
    /// Gets the booking identifier.
    /// </summary>
    public Guid BookingId { get; init; }
}
