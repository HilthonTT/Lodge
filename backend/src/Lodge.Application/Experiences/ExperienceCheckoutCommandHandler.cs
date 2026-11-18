using Lodge.Application.Abstractions.App;
using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Application.Core.Errors;
using Lodge.Application.Payments;
using Lodge.Application.Stripe;
using Lodge.Contracts.Payments;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Experiences;
using Lodge.Domain.Users;
using Microsoft.Extensions.Options;
using Stripe.Checkout;

namespace Lodge.Application.Experiences;

internal sealed class ExperienceCheckoutCommandHandler(
    IUserIdentifierProvider userIdentifierProvider,
    IUserRepository userRepository,
    IExperienceRepository experienceRepository,
    StripeClientProvider stripeClientProvider,
    IPaymentModeProvider paymentModeProvider,
    IOptions<StripeOptions> stripeOptions,
    IOptions<AppOptions> appOptions) : ICommandHandler<ExperienceCheckoutCommand, CheckoutResponse>
{
    internal const string ExperienceBookingIdMetadataKey = "experienceBookingId";

    public async Task<Result<CheckoutResponse>> Handle(ExperienceCheckoutCommand request, CancellationToken cancellationToken)
    {
        bool simulated = paymentModeProvider.Mode == PaymentMode.Simulated;

        if (!simulated && !stripeClientProvider.IsConfigured)
        {
            return Result.Failure<CheckoutResponse>(StripeErrors.NotConfigured);
        }

        Guid userId = userIdentifierProvider.UserId;

        ExperienceBooking? booking = await experienceRepository.GetBookingAsync(request.ExperienceBookingId, cancellationToken);

        if (booking is null || booking.GuestId != userId)
        {
            return Result.Failure<CheckoutResponse>(ExperienceBookingErrors.NotFound(request.ExperienceBookingId));
        }

        if (booking.Status != ExperienceBookingStatus.Reserved)
        {
            return Result.Failure<CheckoutResponse>(ExperienceBookingErrors.NotReserved);
        }

        if (simulated)
        {
            return new CheckoutResponse(
                appOptions.Value.FrontendUrl($"checkout/simulated?experienceBookingId={booking.Id}"));
        }

        User user = (await userRepository.GetByIdAsync(userId, cancellationToken))!;
        Experience? experience = await experienceRepository.GetByIdAsync(booking.ExperienceId, cancellationToken);

        string returnUrl = stripeOptions.Value.ReturnUrl;

        var sessionOptions = new SessionCreateOptions
        {
            Mode = "payment",
            SuccessUrl = returnUrl,
            CancelUrl = returnUrl,
            CustomerEmail = user.Email.Value,
            ClientReferenceId = booking.Id.ToString(),
            LineItems =
            [
                new SessionLineItemOptions
                {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = booking.Currency.ToLowerInvariant(),
                        UnitAmount = StripePaymentGateway.ToMinorUnits(booking.TotalPrice),
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = experience?.Title ?? "Experience",
                            Description = $"{booking.SessionStartsAtUtc:yyyy-MM-dd HH:mm} UTC – {booking.GuestCount} guest(s)",
                        },
                    },
                },
            ],
            Metadata = new Dictionary<string, string>
            {
                [ExperienceBookingIdMetadataKey] = booking.Id.ToString(),
                ["userId"] = userId.ToString(),
            },
        };

        Session session = await new SessionService(stripeClientProvider.Client)
            .CreateAsync(sessionOptions, cancellationToken: cancellationToken);

        return new CheckoutResponse(session.Url);
    }
}
