using Lodge.Application.Abstractions.App;
using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Application.Core.Errors;
using Lodge.Application.Payments;
using Lodge.Contracts.Payments;
using Lodge.Domain.Apartments;
using Lodge.Domain.Bookings;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Users;
using Microsoft.Extensions.Options;
using Stripe.Checkout;

namespace Lodge.Application.Stripe.Checkout;

/// <summary>
/// Starts the payment of a reserved booking: a Stripe Checkout session in Stripe mode, or the frontend's simulated
/// checkout page (<c>{FrontendBaseUrl}/checkout/simulated?bookingId=...</c>) in simulated mode.
/// </summary>
internal sealed class StripeCheckoutCommandHandler(
    IUserIdentifierProvider userIdentifierProvider,
    IUserRepository userRepository,
    IBookingRepository bookingRepository,
    IApartmentRepository apartmentRepository,
    StripeClientProvider stripeClientProvider,
    IPaymentModeProvider paymentModeProvider,
    IOptions<StripeOptions> stripeOptions,
    IOptions<AppOptions> appOptions) : ICommandHandler<StripeCheckoutCommand, CheckoutResponse>
{
    internal const string UserIdMetadataKey = "userId";
    internal const string BookingIdMetadataKey = "bookingId";

    public async Task<Result<CheckoutResponse>> Handle(StripeCheckoutCommand request, CancellationToken cancellationToken)
    {
        if (request.UserId != userIdentifierProvider.UserId)
        {
            return Result.Failure<CheckoutResponse>(UserErrors.InvalidPermissions);
        }

        bool simulated = paymentModeProvider.Mode == PaymentMode.Simulated;

        if (!simulated && !stripeClientProvider.IsConfigured)
        {
            return Result.Failure<CheckoutResponse>(StripeErrors.NotConfigured);
        }

        User? user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure<CheckoutResponse>(UserErrors.NotFound(request.UserId));
        }

        Booking? booking = await bookingRepository.GetByIdAsync(request.BookingId, cancellationToken);

        if (booking is null)
        {
            return Result.Failure<CheckoutResponse>(BookingErrors.NotFound(request.BookingId));
        }

        if (booking.UserId != user.Id)
        {
            return Result.Failure<CheckoutResponse>(UserErrors.InvalidPermissions);
        }

        if (booking.Status != BookingStatus.Reserved)
        {
            return Result.Failure<CheckoutResponse>(StripeErrors.BookingNotPayable);
        }

        if (simulated)
        {
            return new CheckoutResponse(appOptions.Value.FrontendUrl($"checkout/simulated?bookingId={booking.Id}"));
        }

        Apartment? apartment = await apartmentRepository.GetByIdAsync(booking.ApartmentId, cancellationToken);

        if (apartment is null)
        {
            return Result.Failure<CheckoutResponse>(ApartmentErrors.NotFound(booking.ApartmentId));
        }

        string returnUrl = stripeOptions.Value.ReturnUrl;

        var sessionOptions = new SessionCreateOptions
        {
            Mode = "payment",
            SuccessUrl = returnUrl,
            CancelUrl = returnUrl,
            BillingAddressCollection = "auto",
            CustomerEmail = user.Email.Value,
            ClientReferenceId = booking.Id.ToString(),
            LineItems =
            [
                new SessionLineItemOptions
                {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = booking.TotalPrice.Currency.Code.ToLowerInvariant(),
                        // Stripe expects the amount in the smallest currency unit (cents for USD/EUR).
                        UnitAmount = StripePaymentGateway.ToMinorUnits(booking.TotalPrice.Amount),
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = apartment.Name.Value,
                            Description = $"{booking.Duration.Start:yyyy-MM-dd} – {booking.Duration.End:yyyy-MM-dd}",
                        },
                    },
                },
            ],
            Metadata = new Dictionary<string, string>
            {
                [UserIdMetadataKey] = user.Id.ToString(),
                [BookingIdMetadataKey] = booking.Id.ToString(),
            },
        };

        var sessionService = new SessionService(stripeClientProvider.Client);

        Session session = await sessionService.CreateAsync(sessionOptions, cancellationToken: cancellationToken);

        return new CheckoutResponse(session.Url);
    }
}
