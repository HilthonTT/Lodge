using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Messaging;

namespace Lodge.Application.Messaging;

/// <summary>
/// A guest's inquiry about a listing (<c>POST /conversations</c>): reuses the conversation of the guest and the listing
/// or starts it, and posts the message. Not allowed on one's own listing.
/// </summary>
public sealed record StartConversationCommand(Guid ApartmentId, string Message) : ICommand<SentMessageResponse>;
