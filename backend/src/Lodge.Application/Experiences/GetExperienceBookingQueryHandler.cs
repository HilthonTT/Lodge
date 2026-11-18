using System.Data;
using Lodge.Application.Abstractions.App;
using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Experiences;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Experiences;
using Microsoft.Extensions.Options;

namespace Lodge.Application.Experiences;

internal sealed class GetExperienceBookingQueryHandler(
    IUserIdentifierProvider userIdentifierProvider,
    IDbConnectionFactory factory,
    IDateTimeProvider dateTimeProvider,
    IOptions<AppOptions> appOptions) : IQueryHandler<GetExperienceBookingQuery, ExperienceBookingResponse>
{
    public async Task<Result<ExperienceBookingResponse>> Handle(
        GetExperienceBookingQuery request,
        CancellationToken cancellationToken)
    {
        using IDbConnection connection = await factory.GetOpenConnectionAsync(cancellationToken);

        ExperienceBookingResponse? booking = await ExperienceQueries.GetBookingAsync(
            connection,
            request.ExperienceBookingId,
            dateTimeProvider.UtcNow,
            appOptions.Value);

        Guid userId = userIdentifierProvider.UserId;

        if (booking is null || (booking.GuestId != userId && booking.HostId != userId))
        {
            return Result.Failure<ExperienceBookingResponse>(ExperienceBookingErrors.NotFound(request.ExperienceBookingId));
        }

        if (booking.GuestId != userId)
        {
            // The host cannot cancel or review the guest's booking.
            booking = booking with { CanCancel = false, CanReview = false };
        }

        return booking;
    }
}
