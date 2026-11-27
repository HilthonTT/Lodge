using System.Data;
using Lodge.Application.Abstractions.App;
using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Experiences;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Users;
using Microsoft.Extensions.Options;

namespace Lodge.Application.Experiences.GetUserBookings;

internal sealed class GetUserExperienceBookingsQueryHandler(
    IUserIdentifierProvider userIdentifierProvider,
    IDbConnectionFactory factory,
    IDateTimeProvider dateTimeProvider,
    IOptions<AppOptions> appOptions) : IQueryHandler<GetUserExperienceBookingsQuery, List<ExperienceBookingResponse>>
{
    public async Task<Result<List<ExperienceBookingResponse>>> Handle(
        GetUserExperienceBookingsQuery request,
        CancellationToken cancellationToken)
    {
        if (request.UserId != userIdentifierProvider.UserId)
        {
            return Result.Failure<List<ExperienceBookingResponse>>(UserErrors.InvalidPermissions);
        }

        using IDbConnection connection = await factory.GetOpenConnectionAsync(cancellationToken);

        return await ExperienceQueries.GetGuestBookingsAsync(connection, request.UserId, dateTimeProvider.UtcNow, appOptions.Value);
    }
}
