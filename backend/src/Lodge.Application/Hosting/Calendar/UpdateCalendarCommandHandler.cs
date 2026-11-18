using Lodge.Application.Abstractions.Messaging;
using Lodge.Domain.Apartments;
using Lodge.Domain.Core.Primitives;

namespace Lodge.Application.Hosting.Calendar;

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
