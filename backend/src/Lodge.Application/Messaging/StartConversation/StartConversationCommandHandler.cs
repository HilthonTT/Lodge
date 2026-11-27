using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Messaging;
using Lodge.Domain.Apartments;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Messaging;

namespace Lodge.Application.Messaging.StartConversation;

internal sealed class StartConversationCommandHandler(
    IUserIdentifierProvider userIdentifierProvider,
    IApartmentRepository apartmentRepository,
    ConversationService conversationService,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<StartConversationCommand, SentMessageResponse>
{
    public async Task<Result<SentMessageResponse>> Handle(StartConversationCommand request, CancellationToken cancellationToken)
    {
        Guid userId = userIdentifierProvider.UserId;

        Result<string> body = Message.NormalizeBody(request.Message);

        if (body.IsFailure)
        {
            return Result.Failure<SentMessageResponse>(body.Error);
        }

        Apartment? apartment = await apartmentRepository.GetByIdAsync(request.ApartmentId, cancellationToken);

        if (apartment is null)
        {
            return Result.Failure<SentMessageResponse>(ApartmentErrors.NotFound(request.ApartmentId));
        }

        if (apartment.IsOwnedBy(userId))
        {
            return Result.Failure<SentMessageResponse>(MessagingErrors.OwnListing);
        }

        DateTime utcNow = dateTimeProvider.UtcNow;

        Result<Conversation> conversation =
            await conversationService.GetOrStartAsync(apartment.Id, userId, utcNow, cancellationToken);

        if (conversation.IsFailure)
        {
            return Result.Failure<SentMessageResponse>(conversation.Error);
        }

        Result<Message> message = conversationService.Send(conversation.Value, userId, body.Value, utcNow);

        if (message.IsFailure)
        {
            return Result.Failure<SentMessageResponse>(message.Error);
        }

        return new SentMessageResponse(conversation.Value.Id, MessageMapper.ToResponse(message.Value));
    }
}
