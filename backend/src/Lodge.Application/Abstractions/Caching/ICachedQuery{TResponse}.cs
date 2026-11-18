using Lodge.Application.Abstractions.Messaging;

namespace Lodge.Application.Abstractions.Caching;

/// <summary>
/// Represents the cached query.
/// </summary>
/// <typeparam name="TResponse">The response type.</typeparam>
public interface ICachedQuery<TResponse> : IQuery<TResponse>, ICachedQuery
{
}
