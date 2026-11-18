using Lodge.Application.Hosting.Listings;
using Lodge.Application.Hosting.Photos;
using Lodge.Contracts.Apartments;
using Lodge.Contracts.Common;
using Lodge.Contracts.Hosting;
using Lodge.Domain.Apartments;
using Lodge.Domain.Core.Primitives;
using Lodge.Presentation.Extensions;
using Lodge.Presentation.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Lodge.Presentation.Endpoints.Hosting;

/// <summary>
/// The host's listing management. Every endpoint requires a signed-in user with a verified e-mail address
/// (403 <c>User.EmailNotVerified</c>); listings of other hosts give 403 <c>Apartment.NotOwner</c> (admins may manage
/// every listing).
/// </summary>
internal sealed class Listings : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("hosting/photos", async (
            IFormFile? file,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            if (file is null)
            {
                return CustomResults.Problem(Result.Failure(ApartmentErrors.InvalidPhoto));
            }

            await using Stream stream = file.OpenReadStream();

            Result<PhotoUploadResponse> result =
                await sender.Send(new UploadListingPhotoCommand(stream, file.Length), cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Hosting)
        .RequireAuthorization()
        // JWT-authenticated API: no cookies, so antiforgery tokens are not needed for form posts.
        .DisableAntiforgery();

        app.MapGet("hosting/listings", async (
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            Result<PagedList<HostListingSummaryResponse>> result = await sender.Send(
                new GetHostListingsQuery(page ?? Paging.DefaultPage, pageSize ?? Paging.DefaultPageSize),
                cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Hosting)
        .RequireAuthorization();

        app.MapGet("hosting/listings/{apartmentId:guid}", async (
            Guid apartmentId,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            Result<HostListingResponse> result = await sender.Send(new GetHostListingQuery(apartmentId), cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Hosting)
        .RequireAuthorization();

        app.MapPost("hosting/listings", async (
            ListingRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            Result<Guid> result = await sender.Send(new CreateListingCommand(request), cancellationToken);

            return result.Match(
                id => Results.Created($"/api/v1/hosting/listings/{id}", new CreatedResponse(id)),
                CustomResults.Problem);
        })
        .WithTags(Tags.Hosting)
        .RequireAuthorization();

        app.MapPut("hosting/listings/{apartmentId:guid}", async (
            Guid apartmentId,
            ListingRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            Result result = await sender.Send(new UpdateListingCommand(apartmentId, request), cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Hosting)
        .RequireAuthorization();

        app.MapPut("hosting/listings/{apartmentId:guid}/status", async (
            Guid apartmentId,
            UpdateListingStatusRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            Result result = await sender.Send(
                new SetListingStatusCommand(apartmentId, request.IsListed),
                cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Hosting)
        .RequireAuthorization();

        app.MapDelete("hosting/listings/{apartmentId:guid}", async (
            Guid apartmentId,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            Result result = await sender.Send(new DeleteListingCommand(apartmentId), cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Hosting)
        .RequireAuthorization();
    }
}
