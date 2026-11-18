using Microsoft.AspNetCore.SignalR;

namespace Lodge.Presentation.Realtime;

/// <summary>
/// Identifies SignalR users by the access token's <c>userId</c> claim, so that events can target users.
/// </summary>
internal sealed class UserIdClaimUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection) => connection.User?.FindFirst("userId")?.Value;
}
