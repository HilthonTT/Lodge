using Lodge.Contracts.Messaging;
using Lodge.Domain.Messaging;

namespace Lodge.Application.Messaging;

internal static class MessageMapper
{
    public static MessageResponse ToResponse(Message message) => new()
    {
        Id = message.Id,
        ConversationId = message.ConversationId,
        SenderId = message.SenderId,
        Type = message.Type.ToString(),
        Body = message.Body,
        SystemEvent = message.SystemEvent,
        BookingId = message.BookingId,
        SentOnUtc = message.SentOnUtc,
    };
}
