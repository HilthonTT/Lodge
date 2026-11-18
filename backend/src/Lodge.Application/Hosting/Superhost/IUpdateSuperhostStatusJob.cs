namespace Lodge.Application.Hosting.Superhost;

/// <summary>
/// Grants or revokes the Superhost status.
/// </summary>
public interface IUpdateSuperhostStatusJob
{
    /// <summary>
    /// Re-evaluates every host (and every current Superhost) against the requirements.
    /// </summary>
    /// <returns>The number of users whose status changed.</returns>
    Task<int> UpdateAsync(CancellationToken cancellationToken = default);
}
