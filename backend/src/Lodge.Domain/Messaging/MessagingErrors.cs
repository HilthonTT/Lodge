using Lodge.Domain.Core.Primitives;

namespace Lodge.Domain.Messaging;

/// <summary>
/// Contains the messaging errors.
/// </summary>
public static class MessagingErrors
{
    /// <summary>
    /// The conversation does not exist or the caller does not take part in it (both are "not found").
    /// </summary>
    public static Error ConversationNotFound(Guid conversationId) => Error.NotFound(
        "Conversation.NotFound",
        $"The conversation with the Id = '{conversationId}' was not found.");

    public static readonly Error OwnListing = Error.Problem(
        "Conversation.OwnListing",
        "You cannot send an inquiry about your own listing.");

    public static readonly Error EmptyMessage = Error.Validation(
        "Message.Empty",
        "The message must not be empty.");

    public static readonly Error MessageTooLong = Error.Validation(
        "Message.TooLong",
        $"The message must not be longer than {Message.BodyMaxLength} characters.");

    public static readonly Error InvalidCursor = Error.Validation(
        "Message.InvalidCursor",
        "'before' must be a message id or an ISO 8601 date and time.");

    public static readonly Error InvalidLimit = Error.Validation(
        "Message.InvalidLimit",
        $"'limit' must be between 1 and {Message.MaxPageSize}.");
}
