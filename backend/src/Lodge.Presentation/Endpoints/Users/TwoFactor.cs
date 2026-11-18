using Lodge.Application.Users.TwoFactor.Disable;
using Lodge.Application.Users.TwoFactor.Enable;
using Lodge.Application.Users.TwoFactor.RecoveryCodes;
using Lodge.Application.Users.TwoFactor.Setup;
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
/// Two-factor authentication (TOTP) management for the signed-in user.
/// </summary>
internal sealed class TwoFactor : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("users/{userId:guid}/two-factor")
            .WithTags(Tags.Users)
            .RequireAuthorization();

        group.MapPost("setup", async (Guid userId, ISender sender, CancellationToken cancellationToken) =>
        {
            Result<TwoFactorSetupResponse> result = await sender.Send(new SetupTwoFactorCommand(userId), cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        });

        group.MapPost("enable", async (
            Guid userId,
            TwoFactorCodeRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            Result<RecoveryCodesResponse> result = await sender.Send(
                new EnableTwoFactorCommand(userId, request.Code),
                cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        });

        group.MapPost("disable", async (
            Guid userId,
            DisableTwoFactorRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            Result result = await sender.Send(
                new DisableTwoFactorCommand(userId, request.Password, request.Code),
                cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        });

        group.MapPost("recovery-codes", async (
            Guid userId,
            TwoFactorCodeRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            Result<RecoveryCodesResponse> result = await sender.Send(
                new RegenerateRecoveryCodesCommand(userId, request.Code),
                cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        });
    }
}
