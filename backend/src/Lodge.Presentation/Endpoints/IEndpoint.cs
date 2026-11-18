using Microsoft.AspNetCore.Routing;

namespace Lodge.Presentation.Endpoints;

/// <summary>
/// Represents the endpoint interface.
/// </summary>
public interface IEndpoint
{
    /// <summary>
    /// Maps the endpoint.
    /// </summary>
    /// <param name="app">The endpoint router builder.</param>
    void MapEndpoint(IEndpointRouteBuilder app);
}
