using Lodge.Application.Users.GetProfile;
using Lodge.Contracts.Users;
using Lodge.Domain.Core.Primitives;
using Lodge.Presentation.Extensions;
using Lodge.Presentation.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Lodge.Presentation.Endpoints.Users;

/// <summary>
/// The public profile of a user. Anonymous.
/// </summary>
internal sealed class GetProfile : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("users/{userId:guid}/profile", async (
            Guid userId,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            Result<UserProfileResponse> result = await sender.Send(new GetUserProfileQuery(userId), cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Users)
        .AllowAnonymous();
    }
}
