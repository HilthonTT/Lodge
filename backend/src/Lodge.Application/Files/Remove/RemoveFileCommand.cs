using Lodge.Application.Abstractions.Messaging;

namespace Lodge.Application.Files.Remove;

/// <summary>
/// Represents the remove file command.
/// </summary>
/// <param name="UserId">The user identifier.</param>
/// <param name="FileId">The file identifier.</param>
public sealed record RemoveFileCommand(Guid UserId, Guid FileId) : ICommand;
