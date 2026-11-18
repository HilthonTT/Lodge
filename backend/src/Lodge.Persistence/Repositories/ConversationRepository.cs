using Lodge.Domain.Messaging;
using Microsoft.EntityFrameworkCore;

namespace Lodge.Persistence.Repositories;

/// <summary>
/// Represents the conversation repository.
/// </summary>
internal sealed class ConversationRepository(LodgeDbContext context) : IConversationRepository
{
    public Task<Conversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Conversations.FirstOrDefaultAsync(conversation => conversation.Id == id, cancellationToken);

    public Task<Conversation?> GetAsync(Guid apartmentId, Guid guestId, CancellationToken cancellationToken = default) =>
        context.Conversations.FirstOrDefaultAsync(
            conversation => conversation.ApartmentId == apartmentId && conversation.GuestId == guestId,
            cancellationToken);

    public Task<Message?> GetMessageAsync(Guid messageId, CancellationToken cancellationToken = default) =>
        context.Messages.FirstOrDefaultAsync(message => message.Id == messageId, cancellationToken);

    public void Insert(Conversation conversation) => context.Conversations.Add(conversation);

    public void InsertMessage(Message message) => context.Messages.Add(message);
}
