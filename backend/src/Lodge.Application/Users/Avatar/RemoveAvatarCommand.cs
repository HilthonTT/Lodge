using Lodge.Application.Abstractions.Messaging;

namespace Lodge.Application.Users.Avatar;

/// <summary>
/// Removes the user's avatar.
/// </summary>
public sealed record RemoveAvatarCommand(Guid UserId) : ICommand;
