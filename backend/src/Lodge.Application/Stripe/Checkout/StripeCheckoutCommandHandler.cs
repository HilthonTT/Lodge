using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Application.Core.Errors;
using Lodge.Domain.Apartments;
using Lodge.Domain.Bookings;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Users;
using Microsoft.Extensions.Options;
using Stripe.Checkout;

namespace Lodge.Application.Stripe.Checkout;

/// <summary>
/// Creates a Stripe Checkout session for a reserved booking and returns its URL.
/// </summary>
internal sealed class StripeCheckoutCommandHandler(
    IUserIdentifierProvider userIdentifierProvider,
    IUserRepository userRepository,
    IBookingRepository bookingRepository,
    IApartmentRepository apartmentRepository,
    StripeClientProvider stripeClientProvider,
    IOptions<StripeOptions> stripeOptions) : ICommandHandler<StripeCheckoutCommand, string>
{
    internal const string UserIdMetadataKey = "userId";
    internal const string BookingIdMetadataKey = "bookingId";

    public async Task<Result<string>> Handle(StripeCheckoutCommand request, CancellationToken cancellationToken)
    {
        if (request.UserId != userIdentifierProvider.UserId)
        {
            return Result.Failure<string>(UserErrors.InvalidPermissions);
        }

        if (!stripeClientProvider.IsConfigured)
        {
            return Result.Failure<string>(StripeErrors.NotConfigured);
        }

        User? user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure<string>(UserErrors.NotFound(request.UserId));
        }

        Booking? booking = await bookingRepository.GetByIdAsync(request.BookingId, cancellationToken);

        if (booking is null)
        {
            return Result.Failure<string>(BookingErrors.NotFound(request.BookingId));
        }

        if (booking.UserId != user.Id)
        {
            return Result.Failure<string>(UserErrors.InvalidPermissions);
        }

        if (booking.Status != BookingStatus.Reserved)
        {
            return Result.Failure<string>(StripeErrors.BookingNotPayable);
        }

        Apartment? apartment = await apartmentRepository.GetByIdAsync(booking.ApartmentId, cancellationToken);

        if (apartment is null)
        {
            return Result.Failure<string>(ApartmentErrors.NotFound(booking.ApartmentId));
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
                        UnitAmount = (long)Math.Round(booking.TotalPrice.Amount * 100m, MidpointRounding.AwayFromZero),
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

        return session.Url;
    }
}
