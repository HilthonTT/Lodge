using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Users;

namespace Lodge.Application.Users.Avatar;

/// <summary>
/// Uploads a new avatar (JPEG, PNG or WebP, at most 5 MB) and makes it the user's profile picture.
/// </summary>
public sealed record SetAvatarCommand(Guid UserId, Stream Stream, long Length, string? ContentType)
    : ICommand<AvatarResponse>;
