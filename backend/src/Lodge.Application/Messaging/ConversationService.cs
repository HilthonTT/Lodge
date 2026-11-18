using System.Globalization;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Realtime;
using Lodge.Domain.Apartments;
using Lodge.Domain.Bookings;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Messaging;
using Microsoft.EntityFrameworkCore;

namespace Lodge.Application.Messaging;

/// <summary>
/// Finds or starts conversations and posts booking messages. Changes are saved by the caller.
/// </summary>
internal sealed class ConversationService(
    IDbContext context,
    IConversationRepository conversationRepository,
    IRealtimeOutbox realtimeOutbox)
{
    /// <summary>
    /// Gets the conversation of the guest about the listing, or starts it.
    /// </summary>
    public async Task<Result<Conversation>> GetOrStartAsync(
        Guid apartmentId,
        Guid guestId,
        DateTime utcNow,
        CancellationToken cancellationToken,
        Guid? knownHostId = null)
    {
        Conversation? conversation = await conversationRepository.GetAsync(apartmentId, guestId, cancellationToken);

        if (conversation is not null)
        {
            return conversation;
        }

        // Deleted listings keep their conversations, so the query filter is ignored.
        Guid? hostId = knownHostId ?? await context.Set<Apartment>()
            .IgnoreQueryFilters()
            .Where(apartment => apartment.Id == apartmentId)
            .Select(apartment => (Guid?)apartment.OwnerId)
            .FirstOrDefaultAsync(cancellationToken);

        if (hostId is null)
        {
            return Result.Failure<Conversation>(ApartmentErrors.NotFound(apartmentId));
        }

        Result<Conversation> started = Conversation.Start(apartmentId, guestId, hostId.Value, utcNow);

        if (started.IsSuccess)
        {
            conversationRepository.Insert(started.Value);
        }

        return started;
    }

    /// <summary>
    /// Posts a user message from <paramref name="senderId"/> and schedules its realtime push.
    /// </summary>
    public Result<Message> Send(Conversation conversation, Guid senderId, string? body, DateTime utcNow)
    {
        Result<Message> message = conversation.Send(senderId, body, utcNow);

        if (message.IsSuccess)
        {
            conversationRepository.InsertMessage(message.Value);
            realtimeOutbox.MessagePosted(message.Value.Id);
        }

        return message;
    }

    /// <summary>
    /// Posts a system message about the booking in the conversation of its guest and listing (started if needed).
    /// </summary>
    public async Task<Message?> PostSystemMessageAsync(
        Booking booking,
        string systemEvent,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(booking);

        Result<Conversation> conversation = await GetOrStartAsync(booking.ApartmentId, booking.UserId, utcNow, cancellationToken);

        return conversation.IsFailure ? null : Post(conversation.Value, booking, systemEvent, utcNow);
    }

    /// <summary>
    /// Posts a system message about the booking in the conversation.
    /// </summary>
    public Message Post(Conversation conversation, Booking booking, string systemEvent, DateTime utcNow)
    {
        ArgumentNullException.ThrowIfNull(conversation);
        ArgumentNullException.ThrowIfNull(booking);

        Message message = conversation.Post(systemEvent, booking.Id, FallbackText(systemEvent, booking), utcNow);

        conversationRepository.InsertMessage(message);
        realtimeOutbox.MessagePosted(message.Id);

        return message;
    }

    /// <summary>
    /// Opens the conversation of a new booking: the "requested"/"reserved" system message, then the guest's message
    /// to the host (if any), linked to the booking.
    /// </summary>
    public async Task OpenForNewBookingAsync(
        Booking booking,
        Guid hostId,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(booking);

        Result<Conversation> conversation =
            await GetOrStartAsync(booking.ApartmentId, booking.UserId, utcNow, cancellationToken, hostId);

        if (conversation.IsFailure)
        {
            return;
        }

        Post(
            conversation.Value,
            booking,
            booking.Status == BookingStatus.PendingApproval ? SystemEvents.BookingRequested : SystemEvents.BookingReserved,
            utcNow);

        if (booking.MessageToHost is not null)
        {
            Result<Message> message = Send(conversation.Value, booking.UserId, booking.MessageToHost, utcNow.AddMilliseconds(1));

            if (message.IsSuccess)
            {
                message.Value.AttachToBooking(booking.Id);
            }
        }
    }

    /// <summary>
    /// The English fallback of a system message (the frontend localises <see cref="Message.SystemEvent"/>).
    /// </summary>
    internal static string FallbackText(string systemEvent, Booking booking)
    {
        string dates = string.Create(
            CultureInfo.InvariantCulture,
            $"{booking.Duration.Start:MMM d} – {booking.Duration.End:MMM d, yyyy}");

        return systemEvent switch
        {
            SystemEvents.BookingRequested => $"Booking request sent for {dates}.",
            SystemEvents.BookingReserved => $"Booked {dates}. The reservation is confirmed once paid.",
            SystemEvents.BookingAccepted => $"The host accepted the request for {dates}.",
            SystemEvents.BookingDeclined => $"The request for {dates} was declined.",
            SystemEvents.BookingExpired => $"The request for {dates} expired: the host did not respond in time.",
            SystemEvents.BookingConfirmed => $"The booking for {dates} is paid and confirmed.",
            SystemEvents.BookingCancelledByGuest => $"The guest cancelled the booking for {dates}.",
            SystemEvents.BookingCancelledByHost => $"The host cancelled the booking for {dates}.",
            SystemEvents.BookingDatesChanged => $"The booking dates changed to {dates}.",
            _ => systemEvent,
        };
    }
}
