using Lodge.Application.Wishlists;
using Lodge.Contracts.Common;
using Lodge.Contracts.Wishlists;
using Lodge.Domain.Core.Primitives;
using Lodge.Presentation.Extensions;
using Lodge.Presentation.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Lodge.Presentation.Endpoints.Wishlists;

/// <summary>
/// The current user's named wishlists. Other users' wishlists are "not found".
/// </summary>
internal sealed class Wishlists : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("wishlists", async (ISender sender, CancellationToken cancellationToken) =>
        {
            Result<List<WishlistSummaryResponse>> result = await sender.Send(new GetWishlistsQuery(), cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Wishlists)
        .RequireAuthorization();

        app.MapGet("wishlists/saved-apartment-ids", async (ISender sender, CancellationToken cancellationToken) =>
        {
            Result<List<Guid>> result = await sender.Send(new GetSavedApartmentIdsQuery(), cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Wishlists)
        .RequireAuthorization();

        app.MapPost("wishlists", async (
            WishlistNameRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            Result<Guid> result = await sender.Send(new CreateWishlistCommand(request.Name), cancellationToken);

            return result.Match(
                id => Results.Created($"/api/v1/wishlists/{id}", new CreatedResponse(id)),
                CustomResults.Problem);
        })
        .WithTags(Tags.Wishlists)
        .RequireAuthorization();

        app.MapGet("wishlists/{wishlistId:guid}", async (
            Guid wishlistId,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            Result<WishlistResponse> result = await sender.Send(new GetWishlistByIdQuery(wishlistId), cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Wishlists)
        .RequireAuthorization();

        app.MapPut("wishlists/{wishlistId:guid}", async (
            Guid wishlistId,
            WishlistNameRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            Result result = await sender.Send(new RenameWishlistCommand(wishlistId, request.Name), cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Wishlists)
        .RequireAuthorization();

        app.MapDelete("wishlists/{wishlistId:guid}", async (
            Guid wishlistId,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            Result result = await sender.Send(new DeleteWishlistCommand(wishlistId), cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Wishlists)
        .RequireAuthorization();

        app.MapPut("wishlists/{wishlistId:guid}/items/{apartmentId:guid}", async (
            Guid wishlistId,
            Guid apartmentId,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            Result result = await sender.Send(new AddWishlistItemCommand(wishlistId, apartmentId), cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Wishlists)
        .RequireAuthorization();

        app.MapDelete("wishlists/{wishlistId:guid}/items/{apartmentId:guid}", async (
            Guid wishlistId,
            Guid apartmentId,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            Result result = await sender.Send(new RemoveWishlistItemCommand(wishlistId, apartmentId), cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Wishlists)
        .RequireAuthorization();
    }
}
