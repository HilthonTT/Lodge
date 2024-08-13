using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Domain.Apartements;
using Lodge.Domain.Bookings;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Users;
using Microsoft.Extensions.Configuration;
using Stripe.Checkout;

namespace Lodge.Application.Stripe.Checkout;

/// <summary>
/// Represents the <see cref="StripeCheckoutCommand"/> handler.
/// </summary>
/// <param name="userIdentifierProvider">The user identifier provider.</param>
/// <param name="userRepository">The user repository.</param>
/// <param name="bookingRepository">The booking repository.</param>
/// <param name="apartmentRepository">The apartment repository.</param>
internal sealed class StripeCheckoutCommandHandler(
    IUserIdentifierProvider userIdentifierProvider, 
    IUserRepository userRepository,
    IBookingRepository bookingRepository,
    IApartmentRepository apartmentRepository,
    IConfiguration configuration) : ICommandHandler<StripeCheckoutCommand, string>
{
    private readonly string _returnUrl = configuration["Stripe:ReturnUrl"]!;

    /// <inheritdoc />
    public async Task<Result<string>> Handle(StripeCheckoutCommand request, CancellationToken cancellationToken)
    {
        if (request.UserId != userIdentifierProvider.UserId)
        {
            return Result.Failure<string>(UserErrors.InvalidPermissions);
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

        Apartment? apartment = await apartmentRepository.GetByIdAsync(booking.ApartmentId, cancellationToken);
        if (apartment is null)
        {
            return Result.Failure<string>(ApartmentErrors.NotFound(booking.ApartmentId));
        }

        var lineItem = new SessionLineItemOptions()
        {
            PriceData = new SessionLineItemPriceDataOptions()
            {
                Currency = booking.TotalPrice.Currency.Code,
                ProductData = new()
                {
                    Name = apartment.Name.Value,
                    Description = apartment.Description.Value,
                },
                UnitAmount = (long)(booking.TotalPrice.Amount * 100),
            },
            Quantity = 1,
        };

        var sessionOptions = new SessionCreateOptions()
        {
            SuccessUrl = _returnUrl,
            CancelUrl = _returnUrl,
            PaymentMethodTypes = ["card", "paypal"],
            BillingAddressCollection = "auto",
            CustomerEmail = user.Email.Value,
            LineItems =
            [
                lineItem
            ],
            Metadata = new Dictionary<string, string>()
            {
                { "userId", user.Id.ToString() },
                { "bookingId", booking.Id.ToString() }
            }
        };

        var sessionService = new SessionService();

        Session stripeSession = await sessionService.CreateAsync(sessionOptions, cancellationToken: cancellationToken);

        return Result.Success(stripeSession.Url);
    }
}
