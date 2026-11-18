using FluentValidation;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Application.Apartments.GetCalendar;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;
using Lodge.Application.Hosting.Listings;
using Lodge.Domain.Apartments;
using Lodge.Domain.Core.Primitives;

namespace Lodge.Application.Hosting.Calendar;

/// <summary>
/// Blocks/unblocks nights and/or sets/clears custom nightly prices for every night from <paramref name="From"/> to
/// <paramref name="To"/> (inclusive, at most 366 days, not in the past). Booked nights may be blocked too; that has no
/// effect on the existing bookings.
/// </summary>
/// <param name="ApartmentId">The listing.</param>
/// <param name="From">The first night.</param>
/// <param name="To">The last night (inclusive).</param>
/// <param name="Blocked">True blocks, false unblocks, null leaves blocks unchanged.</param>
/// <param name="Price">A custom nightly price, or null to leave prices unchanged.</param>
/// <param name="ClearPrice">True removes the custom prices.</param>
public sealed record UpdateCalendarCommand(
    Guid ApartmentId,
    DateOnly From,
    DateOnly To,
    bool? Blocked,
    decimal? Price,
    bool ClearPrice) : ICommand;

internal sealed class UpdateCalendarCommandValidator : AbstractValidator<UpdateCalendarCommand>
{
    public UpdateCalendarCommandValidator()
    {
        RuleFor(x => x)
            .Must(x => CalendarRange.IsValid(x.From, x.To))
            .WithError(ApartmentErrors.InvalidCalendarRange);

        RuleFor(x => x)
            .Must(x => x.Blocked.HasValue || x.Price.HasValue || x.ClearPrice)
            .WithError(ValidationErrors.Calendar.NothingToChange);

        RuleFor(x => x)
            .Must(x => !(x.Price.HasValue && x.ClearPrice))
            .WithError(ValidationErrors.Calendar.PriceAndClearPrice);

        RuleFor(x => x.Price)
            .Must(price => price is null or (> 0 and <= ListingRequestValidator.MaxPrice))
            .WithError(ApartmentErrors.InvalidPrice);
    }
}

internal sealed class UpdateCalendarCommandHandler(
    IHostAccess hostAccess,
    IApartmentRepository apartmentRepository,
    ICalendarDayRepository calendarDayRepository,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<UpdateCalendarCommand>
{
    public async Task<Result> Handle(UpdateCalendarCommand request, CancellationToken cancellationToken)
    {
        Result<HostCaller> caller = await hostAccess.GetCallerAsync(cancellationToken);

        if (caller.IsFailure)
        {
            return caller;
        }

        Apartment? apartment = await apartmentRepository.GetByIdAsync(request.ApartmentId, cancellationToken);

        if (apartment is null)
        {
            return Result.Failure(ApartmentErrors.NotFound(request.ApartmentId));
        }

        Result access = caller.Value.EnsureCanManage(apartment);

        if (access.IsFailure)
        {
            return access;
        }

        if (request.From < DateOnly.FromDateTime(dateTimeProvider.UtcNow))
        {
            return Result.Failure(ApartmentErrors.CalendarInPast);
        }

        Dictionary<DateOnly, CalendarDay> existing = (await calendarDayRepository.GetRangeAsync(
                apartment.Id,
                request.From,
                request.To,
                cancellationToken))
            .ToDictionary(day => day.Date);

        for (DateOnly date = request.From; date <= request.To; date = date.AddDays(1))
        {
            bool isNew = !existing.TryGetValue(date, out CalendarDay? day);
            day ??= CalendarDay.Create(apartment.Id, date);

            if (request.Blocked is bool blocked)
            {
                day.SetBlocked(blocked);
            }

            if (request.Price is decimal price)
            {
                day.SetPrice(price);
            }
            else if (request.ClearPrice)
            {
                day.SetPrice(null);
            }

            if (day.IsEmpty && !isNew)
            {
                calendarDayRepository.Remove(day);
            }
            else if (!day.IsEmpty && isNew)
            {
                calendarDayRepository.Insert(day);
            }
        }

        return Result.Success();
    }
}
