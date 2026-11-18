using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Messaging;

namespace Lodge.Application.Messaging;

/// <summary>
/// Gets one of the caller's conversations (<c>GET /conversations/{id}</c>).
/// </summary>
public sealed record GetConversationByIdQuery(Guid ConversationId) : IQuery<ConversationResponse>;
