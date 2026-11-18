using Lodge.Application.Bookings.Expire;
using Microsoft.Extensions.Options;
using System.Data;
using FluentValidation;
using Lodge.Application.Abstractions.Assets;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Application.Bookings;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;
using Lodge.Contracts.Bookings;
using Lodge.Contracts.Common;
using Lodge.Domain.Bookings;
using Lodge.Domain.Core.Primitives;

namespace Lodge.Application.Hosting.Bookings;

/// <summary>
/// Gets one page of the bookings of the current user's listings, newest first.
/// </summary>
/// <param name="Status">An optional status filter (1 … 7).</param>
/// <param name="ListingId">An optional listing filter.</param>
/// <param name="Page">The page.</param>
/// <param name="PageSize">The page size.</param>
public sealed record GetHostBookingsQuery(int? Status, Guid? ListingId, int Page, int PageSize)
    : IQuery<PagedList<BookingResponse>>;

internal sealed class GetHostBookingsQueryValidator : AbstractValidator<GetHostBookingsQuery>
{
    public GetHostBookingsQueryValidator()
    {
        RuleFor(x => x.Page).ValidPage();
        RuleFor(x => x.PageSize).ValidPageSize();

        RuleFor(x => x.Status)
            .Must(status => status is null || Enum.IsDefined((BookingStatus)status.Value))
            .WithError(ValidationErrors.GetBookings.InvalidStatus);
    }
}

internal sealed class GetHostBookingsQueryHandler(
    IHostAccess hostAccess,
    IDbConnectionFactory factory,
    IAssetUrlResolver assetUrlResolver,
    IDateTimeProvider dateTimeProvider,
    IOptions<ExpireBookingRequestsOptions> expiryOptions) : IQueryHandler<GetHostBookingsQuery, PagedList<BookingResponse>>
{
    public async Task<Result<PagedList<BookingResponse>>> Handle(
        GetHostBookingsQuery request,
        CancellationToken cancellationToken)
    {
        Result<HostCaller> caller = await hostAccess.GetCallerAsync(cancellationToken);

        if (caller.IsFailure)
        {
            return Result.Failure<PagedList<BookingResponse>>(caller.Error);
        }

        using IDbConnection connection = await factory.GetOpenConnectionAsync(cancellationToken);

        (List<BookingResponse> items, int totalCount) = await BookingQueries.GetPageAsync(
            connection,
            request.Status,
            request.Page,
            request.PageSize,
            hostId: caller.Value.UserId,
            apartmentId: request.ListingId);

        DateOnly today = DateOnly.FromDateTime(dateTimeProvider.UtcNow);

        foreach (BookingResponse booking in items)
        {
            BookingQueries.Present(booking, caller.Value.UserId, assetUrlResolver, today, expiryOptions.Value.ExpiryHours);
        }

        return new PagedList<BookingResponse>(items, request.Page, request.PageSize, totalCount);
    }
}
