using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Application.Core.Errors;
using Lodge.Contracts.Payments;
using Lodge.Domain.Bookings;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Users;
using Microsoft.Extensions.Options;

namespace Lodge.Application.Payments;

/// <summary>
/// Completes the simulated checkout of the caller's reserved booking: it is confirmed exactly as the Stripe webhook
/// would (same domain path and events) with a fake payment id. Only available in simulated mode.
/// </summary>
public sealed record CompleteSimulatedPaymentCommand(Guid BookingId) : ICommand;

internal sealed class CompleteSimulatedPaymentCommandHandler(
    IUserIdentifierProvider userIdentifierProvider,
    IBookingRepository bookingRepository,
    IPaymentModeProvider paymentModeProvider,
    BookingPayments bookingPayments) : ICommandHandler<CompleteSimulatedPaymentCommand>
{
    public async Task<Result> Handle(CompleteSimulatedPaymentCommand request, CancellationToken cancellationToken)
    {
        if (paymentModeProvider.Mode != PaymentMode.Simulated)
        {
            return Result.Failure(PaymentErrors.NotSimulated);
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

        if (booking.Status is not (BookingStatus.Reserved or BookingStatus.Confirmed or BookingStatus.Completed))
        {
            return Result.Failure(StripeErrors.BookingNotPayable);
        }

        return await bookingPayments.ConfirmPaymentAsync(
            booking,
            SimulatedIds.New("pi"),
            booking.TotalPrice.Amount,
            cancellationToken);
    }
}

/// <summary>
/// Gets the payment mode (<c>GET /payments/config</c>).
/// </summary>
public sealed record GetPaymentsConfigQuery : IQuery<PaymentsConfigResponse>;

internal sealed class GetPaymentsConfigQueryHandler(IPaymentModeProvider paymentModeProvider)
    : IQueryHandler<GetPaymentsConfigQuery, PaymentsConfigResponse>
{
    public Task<Result<PaymentsConfigResponse>> Handle(GetPaymentsConfigQuery request, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success(new PaymentsConfigResponse(paymentModeProvider.Mode.ToString())));
}

/// <summary>
/// Gets the static display exchange rates (<c>GET /currencies</c>).
/// </summary>
public sealed record GetCurrenciesQuery : IQuery<CurrenciesResponse>;

internal sealed class GetCurrenciesQueryHandler(IOptions<CurrencyOptions> options)
    : IQueryHandler<GetCurrenciesQuery, CurrenciesResponse>
{
    public Task<Result<CurrenciesResponse>> Handle(GetCurrenciesQuery request, CancellationToken cancellationToken)
    {
        CurrencyOptions currencies = options.Value;
        string baseCurrency = currencies.Base.Trim().ToUpperInvariant();

        var rates = new SortedDictionary<string, decimal>(StringComparer.Ordinal) { [baseCurrency] = 1m };

        foreach ((string code, decimal rate) in currencies.Rates)
        {
            if (rate > 0)
            {
                rates[code.Trim().ToUpperInvariant()] = rate;
            }
        }

        rates[baseCurrency] = 1m;

        return Task.FromResult(Result.Success(new CurrenciesResponse(baseCurrency, rates, currencies.UpdatedOnUtc)));
    }
}
