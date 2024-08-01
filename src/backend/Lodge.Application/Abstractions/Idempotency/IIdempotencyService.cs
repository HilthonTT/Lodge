namespace Lodge.Application.Abstractions.Idempotency;

/// <summary>
/// Represents the idempotency service interface.
/// </summary>
public interface IIdempotencyService
{
    /// <summary>
    /// Checks if the request exists or not.
    /// </summary>
    /// <param name="requestId">The request identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the request exists, otherwise false.</returns>
    Task<bool> RequestExistsAsync(Guid requestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates an idempotent request.
    /// </summary>
    /// <param name="requestId">The request identifier.</param>
    /// <param name="name">The name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The completed task.</returns>
    Task CreateRequestAsync(Guid requestId, string name, CancellationToken cancellationToken = default);
}
