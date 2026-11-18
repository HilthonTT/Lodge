namespace Lodge.Domain.Messaging;

/// <summary>
/// Represents the repository of conversations and messages.
/// </summary>
public interface IConversationRepository
{
    Task<Conversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Conversation?> GetAsync(Guid apartmentId, Guid guestId, CancellationToken cancellationToken = default);

    Task<Message?> GetMessageAsync(Guid messageId, CancellationToken cancellationToken = default);

    void Insert(Conversation conversation);

    void InsertMessage(Message message);
}
