using System.Data;
using FluentValidation;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Application.Apartments;
using Lodge.Application.Apartments.GetCalendar;
using Lodge.Application.Core.Extensions;
using Lodge.Contracts.Apartments;
using Lodge.Domain.Apartments;
using Lodge.Domain.Core.Primitives;

namespace Lodge.Application.Hosting.Calendar;

/// <summary>
/// Gets the host's view of a listing's calendar from <paramref name="From"/> to <paramref name="To"/> (inclusive,
/// at most 366 days).
/// </summary>
public sealed record GetHostCalendarQuery(Guid ApartmentId, DateOnly From, DateOnly To)
    : IQuery<List<HostCalendarDayResponse>>;

internal sealed class GetHostCalendarQueryValidator : AbstractValidator<GetHostCalendarQuery>
{
    public GetHostCalendarQueryValidator() =>
        RuleFor(x => x)
            .Must(x => CalendarRange.IsValid(x.From, x.To))
            .WithError(ApartmentErrors.InvalidCalendarRange);
}

internal sealed class GetHostCalendarQueryHandler(
    IHostAccess hostAccess,
    IApartmentRepository apartmentRepository,
    IDbConnectionFactory factory,
    IDateTimeProvider dateTimeProvider) : IQueryHandler<GetHostCalendarQuery, List<HostCalendarDayResponse>>
{
    public async Task<Result<List<HostCalendarDayResponse>>> Handle(
        GetHostCalendarQuery request,
        CancellationToken cancellationToken)
    {
        Result<HostCaller> caller = await hostAccess.GetCallerAsync(cancellationToken);

        if (caller.IsFailure)
        {
            return Result.Failure<List<HostCalendarDayResponse>>(caller.Error);
        }

        Apartment? apartment = await apartmentRepository.GetByIdAsync(request.ApartmentId, cancellationToken);

        if (apartment is null)
        {
            return Result.Failure<List<HostCalendarDayResponse>>(ApartmentErrors.NotFound(request.ApartmentId));
        }

        Result access = caller.Value.EnsureCanManage(apartment);

        if (access.IsFailure)
        {
            return Result.Failure<List<HostCalendarDayResponse>>(access.Error);
        }

        using IDbConnection connection = await factory.GetOpenConnectionAsync(cancellationToken);

        ApartmentQueries.ListingNights nights =
            await ApartmentQueries.GetNightsAsync(connection, apartment.Id, request.From, request.To);

        DateOnly today = DateOnly.FromDateTime(dateTimeProvider.UtcNow);

        return ApartmentQueries.BuildCalendar(apartment, nights, request.From, request.To, today)
            .Select(night => new HostCalendarDayResponse(
                night.Date,
                night.Available,
                night.Blocked,
                night.Booked,
                night.Price,
                night.CustomPrice))
            .ToList();
    }
}
