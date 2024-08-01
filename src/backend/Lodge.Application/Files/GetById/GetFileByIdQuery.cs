using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Storage;

namespace Lodge.Application.Files.GetById;

/// <summary>
/// Represents the query for fetching the file by its identifier.
/// </summary>
/// <param name="FileId">The file identifier.</param>
public sealed record GetFileByIdQuery(Guid FileId) : IQuery<FileResponse>;
