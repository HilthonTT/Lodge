using Lodge.Application.Users.Avatar;
using Lodge.Contracts.Users;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Users;
using Lodge.Presentation.Extensions;
using Lodge.Presentation.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Lodge.Presentation.Endpoints.Users;

/// <summary>
/// Sets (multipart <c>file</c>: JPEG, PNG or WebP, max. 5 MB) or removes the user's avatar.
/// </summary>
internal sealed class Avatar : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("users/{userId:guid}/avatar", async (
            Guid userId,
            IFormFile? file,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            if (file is null)
            {
                return CustomResults.Problem(Result.Failure(UserErrors.InvalidAvatar));
            }

            await using Stream stream = file.OpenReadStream();

            Result<AvatarResponse> result = await sender.Send(
                new SetAvatarCommand(userId, stream, file.Length, file.ContentType),
                cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Users)
        .RequireAuthorization()
        // JWT-authenticated API: no cookies, so antiforgery tokens are not needed for form posts.
        .DisableAntiforgery();

        app.MapDelete("users/{userId:guid}/avatar", async (
            Guid userId,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            Result result = await sender.Send(new RemoveAvatarCommand(userId), cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Users)
        .RequireAuthorization();
    }
}
