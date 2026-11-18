using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Bookings;
using Lodge.Domain.Bookings;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Users;

namespace Lodge.Application.Bookings.Cancel;

internal sealed class GetCancellationQuoteQueryHandler(
    IUserIdentifierProvider userIdentifierProvider,
    IBookingRepository bookingRepository,
    IDateTimeProvider dateTimeProvider) : IQueryHandler<GetCancellationQuoteQuery, CancellationQuoteResponse>
{
    public async Task<Result<CancellationQuoteResponse>> Handle(
        GetCancellationQuoteQuery request,
        CancellationToken cancellationToken)
    {
        Booking? booking = await bookingRepository.GetByIdAsync(request.BookingId, cancellationToken);

        if (booking is null)
        {
            return Result.Failure<CancellationQuoteResponse>(BookingErrors.NotFound(request.BookingId));
        }

        if (booking.UserId != userIdentifierProvider.UserId)
        {
            return Result.Failure<CancellationQuoteResponse>(UserErrors.InvalidPermissions);
        }

        DateTime utcNow = dateTimeProvider.UtcNow;
        Result cancellable = booking.EnsureCancellable(utcNow);

        if (cancellable.IsFailure)
        {
            return Result.Failure<CancellationQuoteResponse>(cancellable.Error);
        }

        RefundQuote quote = RefundPolicy.Quote(booking, DateOnly.FromDateTime(utcNow));

        return new CancellationQuoteResponse(
            quote.Policy.ToString(),
            quote.RefundPercent,
            quote.RefundAmount,
            quote.NonRefundableAmount,
            booking.TotalPrice.Currency.Code,
            [.. quote.Timeline.Select(tier => new RefundTimelineEntryResponse(tier.Until, tier.RefundPercent))],
            quote.IsPaid);
    }
}
