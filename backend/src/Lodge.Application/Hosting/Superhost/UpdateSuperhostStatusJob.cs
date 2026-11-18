using System.Data;
using Dapper;
using Lodge.Application.Abstractions.Caching;
using Lodge.Application.Abstractions.Data;
using Lodge.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lodge.Application.Hosting.Superhost;

/// <summary>
/// Daily job: a host is a Superhost while their track record (<see cref="HostPerformance"/>) meets the configured
/// <see cref="SuperhostOptions"/>. The cached apartments of affected hosts are evicted, since they show the badge.
/// </summary>
internal sealed partial class UpdateSuperhostStatusJob(
    IDbConnectionFactory factory,
    IDbContext context,
    IUnitOfWork unitOfWork,
    ICacheService cacheService,
    IOptions<SuperhostOptions> options,
    ILogger<UpdateSuperhostStatusJob> logger) : IUpdateSuperhostStatusJob
{
    public async Task<int> UpdateAsync(CancellationToken cancellationToken = default)
    {
        SuperhostRequirements requirements = options.Value.ToRequirements();

        Dictionary<Guid, HostPerformance> performances;

        using (IDbConnection connection = await factory.GetOpenConnectionAsync(cancellationToken))
        {
            performances = await HostPerformanceQueries.GetAsync(connection);
        }

        Guid[] hostIds = [.. performances.Keys];

        List<User> users = await context.Set<User>()
            .Where(user => user.IsSuperhost || hostIds.Contains(user.Id))
            .ToListAsync(cancellationToken);

        List<Guid> changed = [];

        foreach (User user in users)
        {
            bool meets = performances.TryGetValue(user.Id, out HostPerformance? performance) &&
                         performance.Meets(requirements);

            if (user.IsSuperhost != meets)
            {
                user.SetSuperhost(meets);
                changed.Add(user.Id);
            }
        }

        if (changed.Count == 0)
        {
            LogUpdated(logger, 0);
            return 0;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        using (IDbConnection connection = await factory.GetOpenConnectionAsync(cancellationToken))
        {
            IEnumerable<Guid> apartmentIds = await connection.QueryAsync<Guid>(
                "SELECT a.id FROM apartments AS a WHERE a.owner_id = ANY(@OwnerIds)",
                new { OwnerIds = changed.ToArray() });

            foreach (Guid apartmentId in apartmentIds)
            {
                await cacheService.RemoveAsync(CacheKeys.Apartments.GetById(apartmentId), cancellationToken);
            }
        }

        LogUpdated(logger, changed.Count);

        return changed.Count;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Superhost status changed for {Count} users")]
    private static partial void LogUpdated(ILogger logger, int count);
}
