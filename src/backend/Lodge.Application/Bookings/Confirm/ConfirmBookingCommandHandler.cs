﻿using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Domain.Bookings;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Users;
using MediatR;

namespace Lodge.Application.Bookings.Confirm;

/// <summary>
/// Represents the <see cref="ConfirmBookingCommand"/> handler.
/// </summary>
/// <param name="userIdentifierProvider">The user identifier.</param>
/// <param name="bookingRepository">The booking repository.</param>
/// <param name="dateTimeProvider">The date time provider.</param>
/// <param name="unitOfWork">The unit of work.</param>
/// <param name="publisher">The publisher.</param>
internal sealed class ConfirmBookingCommandHandler(
    IUserIdentifierProvider userIdentifierProvider,
    IBookingRepository bookingRepository,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork,
    IPublisher publisher) : ICommandHandler<ConfirmBookingCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(ConfirmBookingCommand request, CancellationToken cancellationToken)
    {
        if (request.UserId != userIdentifierProvider.UserId)
        {
            return Result.Failure(UserErrors.InvalidPermissions);
        }

        Booking? booking = await bookingRepository.GetByIdAsync(request.BookingId, cancellationToken);
        if (booking is null)
        {
            return Result.Failure(BookingErrors.NotFound(request.BookingId));
        }

        if (booking.UserId != userIdentifierProvider.UserId)
        {
            return Result.Failure(UserErrors.InvalidPermissions);
        }

        Result result = booking.Confirm(dateTimeProvider.UtcNow);
        if (result.IsFailure)
        {
            return Result.Failure(result.Error);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await publisher.Publish(
            new BookingConfirmedEvent(booking.Id, userIdentifierProvider.UserId),
            cancellationToken);

        return Result.Success();
    }
}
