using Lodge.Application.Abstractions.Messaging;
using Lodge.Domain.Bookings.Events;
using Newtonsoft.Json;

namespace Lodge.Application.Bookings.Complete;

/// <summary>
/// Represents the integration event that is raised when a booking is completed.
/// </summary>
public sealed class BookingCompletedIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BookingCompletedIntegrationEvent"/> class.
    /// </summary>
    internal BookingCompletedIntegrationEvent(BookingCompletedDomainEvent bookingCompletedDomainEvent)
    {
        BookingId = bookingCompletedDomainEvent.BookingId;
    }

    [JsonConstructor]
    private BookingCompletedIntegrationEvent(Guid bookingId) => BookingId = bookingId;

    /// <summary>
    /// Gets the booking identifier.
    /// </summary>
    public Guid BookingId { get; }
}
