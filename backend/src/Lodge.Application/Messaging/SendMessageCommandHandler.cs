using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Messaging;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Messaging;

namespace Lodge.Application.Messaging;

internal sealed class SendMessageCommandHandler(
    IUserIdentifierProvider userIdentifierProvider,
    IConversationRepository conversationRepository,
    ConversationService conversationService,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<SendMessageCommand, MessageResponse>
{
    public async Task<Result<MessageResponse>> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        Guid userId = userIdentifierProvider.UserId;

        Conversation? conversation = await conversationRepository.GetByIdAsync(request.ConversationId, cancellationToken);

        if (conversation is null || !conversation.IsParticipant(userId))
        {
            return Result.Failure<MessageResponse>(MessagingErrors.ConversationNotFound(request.ConversationId));
        }

        Result<Message> message = conversationService.Send(conversation, userId, request.Body, dateTimeProvider.UtcNow);

        return message.IsSuccess
            ? MessageMapper.ToResponse(message.Value)
            : Result.Failure<MessageResponse>(message.Error);
    }
}
