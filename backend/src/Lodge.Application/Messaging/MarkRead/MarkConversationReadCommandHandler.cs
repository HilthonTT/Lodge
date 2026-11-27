using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Application.Abstractions.Realtime;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Messaging;

namespace Lodge.Application.Messaging.MarkRead;

internal sealed class MarkConversationReadCommandHandler(
    IUserIdentifierProvider userIdentifierProvider,
    IConversationRepository conversationRepository,
    IRealtimeOutbox realtimeOutbox,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<MarkConversationReadCommand>
{
    public async Task<Result> Handle(MarkConversationReadCommand request, CancellationToken cancellationToken)
    {
        Guid userId = userIdentifierProvider.UserId;

        Conversation? conversation = await conversationRepository.GetByIdAsync(request.ConversationId, cancellationToken);

        if (conversation is null || !conversation.IsParticipant(userId))
        {
            return Result.Failure(MessagingErrors.ConversationNotFound(request.ConversationId));
        }

        conversation.MarkRead(userId, dateTimeProvider.UtcNow);
        realtimeOutbox.ConversationChanged(conversation.Id);

        return Result.Success();
    }
}
