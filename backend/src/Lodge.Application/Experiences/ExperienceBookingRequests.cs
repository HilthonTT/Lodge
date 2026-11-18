using System.Data;
using Lodge.Application.Abstractions.App;
using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Idempotency;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Application.Core.Errors;
using Lodge.Application.Payments;
using Lodge.Application.Stripe;
using Lodge.Contracts.Experiences;
using Lodge.Contracts.Payments;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Experiences;
using Lodge.Domain.Users;
using Microsoft.Extensions.Options;
using Stripe.Checkout;

namespace Lodge.Application.Experiences;

/// <summary>
/// Reserves spots in a session of a listed experience (verified e-mail required). The spots are held until the booking
/// is paid (<c>POST /experience-bookings/{id}/checkout</c>) or the hold expires. The session row is locked while the
/// spots are counted, so concurrent bookings never overbook it.
/// </summary>
public sealed record ReserveExperienceCommand(Guid RequestId, Guid SessionId, int GuestCount)
    : IdempotentCommand<Guid>(RequestId);

internal sealed class ReserveExperienceCommandHandler(
    IUserIdentifierProvider userIdentifierProvider,
    IUserRepository userRepository,
    IExperienceRepository experienceRepository,
    ExperiencePayments experiencePayments,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<ReserveExperienceCommand, Guid>
{
    public async Task<Result<Guid>> Handle(ReserveExperienceCommand request, CancellationToken cancellationToken)
    {
        Guid userId = userIdentifierProvider.UserId;

        User? user = await userRepository.GetByIdAsync(userId, cancellationToken);

        if (user is null)
        {
            return Result.Failure<Guid>(UserErrors.NotFound(userId));
        }

        if (!user.IsEmailVerified)
        {
            return Result.Failure<Guid>(UserErrors.EmailNotVerified);
        }

        ExperienceSession? session = await experienceRepository.GetSessionForUpdateAsync(request.SessionId, cancellationToken);

        if (session is null)
        {
            return Result.Failure<Guid>(ExperienceSessionErrors.NotFound(request.SessionId));
        }

        Experience? experience = await experienceRepository.GetByIdAsync(session.ExperienceId, cancellationToken);

        if (experience is not { IsListed: true })
        {
            return Result.Failure<Guid>(ExperienceSessionErrors.NotFound(request.SessionId));
        }

        Result<ExperienceBooking> booking = ExperienceBooking.Reserve(
            experience,
            session,
            userId,
            request.GuestCount,
            experiencePayments.GuestServiceFeePercent,
            dateTimeProvider.UtcNow);

        if (booking.IsFailure)
        {
            return Result.Failure<Guid>(booking.Error);
        }

        experienceRepository.InsertBooking(booking.Value);

        return booking.Value.Id;
    }
}

/// <summary>
/// Starts the payment of a reserved experience booking: a Stripe Checkout session in Stripe mode, or the frontend's
/// simulated checkout page (<c>{FrontendBaseUrl}/checkout/simulated?experienceBookingId=…</c>).
/// </summary>
public sealed record ExperienceCheckoutCommand(Guid ExperienceBookingId) : ICommand<CheckoutResponse>;

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

/// <summary>
/// Completes the simulated checkout of the caller's reserved experience booking (simulated mode only), exactly as the
/// Stripe webhook would.
/// </summary>
public sealed record CompleteSimulatedExperiencePaymentCommand(Guid ExperienceBookingId) : ICommand;

internal sealed class CompleteSimulatedExperiencePaymentCommandHandler(
    IUserIdentifierProvider userIdentifierProvider,
    IExperienceRepository experienceRepository,
    IPaymentModeProvider paymentModeProvider,
    ExperiencePayments experiencePayments) : ICommandHandler<CompleteSimulatedExperiencePaymentCommand>
{
    public async Task<Result> Handle(CompleteSimulatedExperiencePaymentCommand request, CancellationToken cancellationToken)
    {
        if (paymentModeProvider.Mode != PaymentMode.Simulated)
        {
            return Result.Failure(PaymentErrors.NotSimulated);
        }

        ExperienceBooking? booking = await experienceRepository.GetBookingAsync(request.ExperienceBookingId, cancellationToken);

        if (booking is null || booking.GuestId != userIdentifierProvider.UserId)
        {
            return Result.Failure(ExperienceBookingErrors.NotFound(request.ExperienceBookingId));
        }

        return await experiencePayments.ConfirmPaymentAsync(
            booking,
            SimulatedIds.New("pi"),
            booking.TotalPrice,
            cancellationToken);
    }
}

/// <summary>
/// Gets an experience booking: its guest and the experience's host only (others get 404).
/// </summary>
public sealed record GetExperienceBookingQuery(Guid ExperienceBookingId) : IQuery<ExperienceBookingResponse>;

internal sealed class GetExperienceBookingQueryHandler(
    IUserIdentifierProvider userIdentifierProvider,
    IDbConnectionFactory factory,
    IDateTimeProvider dateTimeProvider,
    IOptions<AppOptions> appOptions) : IQueryHandler<GetExperienceBookingQuery, ExperienceBookingResponse>
{
    public async Task<Result<ExperienceBookingResponse>> Handle(
        GetExperienceBookingQuery request,
        CancellationToken cancellationToken)
    {
        using IDbConnection connection = await factory.GetOpenConnectionAsync(cancellationToken);

        ExperienceBookingResponse? booking = await ExperienceQueries.GetBookingAsync(
            connection,
            request.ExperienceBookingId,
            dateTimeProvider.UtcNow,
            appOptions.Value);

        Guid userId = userIdentifierProvider.UserId;

        if (booking is null || (booking.GuestId != userId && booking.HostId != userId))
        {
            return Result.Failure<ExperienceBookingResponse>(ExperienceBookingErrors.NotFound(request.ExperienceBookingId));
        }

        if (booking.GuestId != userId)
        {
            // The host cannot cancel or review the guest's booking.
            booking = booking with { CanCancel = false, CanReview = false };
        }

        return booking;
    }
}

/// <summary>
/// Gets the experience bookings of the caller (<c>GET /users/{userId}/experience-bookings</c>), latest session first.
/// </summary>
public sealed record GetUserExperienceBookingsQuery(Guid UserId) : IQuery<List<ExperienceBookingResponse>>;

internal sealed class GetUserExperienceBookingsQueryHandler(
    IUserIdentifierProvider userIdentifierProvider,
    IDbConnectionFactory factory,
    IDateTimeProvider dateTimeProvider,
    IOptions<AppOptions> appOptions) : IQueryHandler<GetUserExperienceBookingsQuery, List<ExperienceBookingResponse>>
{
    public async Task<Result<List<ExperienceBookingResponse>>> Handle(
        GetUserExperienceBookingsQuery request,
        CancellationToken cancellationToken)
    {
        if (request.UserId != userIdentifierProvider.UserId)
        {
            return Result.Failure<List<ExperienceBookingResponse>>(UserErrors.InvalidPermissions);
        }

        using IDbConnection connection = await factory.GetOpenConnectionAsync(cancellationToken);

        return await ExperienceQueries.GetGuestBookingsAsync(connection, request.UserId, dateTimeProvider.UtcNow, appOptions.Value);
    }
}

/// <summary>
/// The guest cancels an experience booking before the session: a full refund at least 24 hours before the start,
/// otherwise none (the refund is sent by the background refund pipeline).
/// </summary>
public sealed record CancelExperienceBookingCommand(Guid ExperienceBookingId) : ICommand;

internal sealed class CancelExperienceBookingCommandHandler(
    IUserIdentifierProvider userIdentifierProvider,
    IExperienceRepository experienceRepository,
    ExperiencePayments experiencePayments,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<CancelExperienceBookingCommand>
{
    public async Task<Result> Handle(CancelExperienceBookingCommand request, CancellationToken cancellationToken)
    {
        ExperienceBooking? booking = await experienceRepository.GetBookingAsync(request.ExperienceBookingId, cancellationToken);

        if (booking is null || booking.GuestId != userIdentifierProvider.UserId)
        {
            return Result.Failure(ExperienceBookingErrors.NotFound(request.ExperienceBookingId));
        }

        ExperienceSession? session = await experienceRepository.GetSessionForUpdateAsync(booking.SessionId, cancellationToken);

        if (session is null)
        {
            return Result.Failure(ExperienceSessionErrors.NotFound(booking.SessionId));
        }

        Result<decimal> cancelled = booking.Cancel(session, dateTimeProvider.UtcNow, byHost: false);

        if (cancelled.IsFailure)
        {
            return cancelled;
        }

        await experiencePayments.AdjustPayoutAsync(booking, cancelled.Value, cancellationToken);

        return Result.Success();
    }
}

/// <summary>
/// The guest reviews a completed experience booking, once (published right away).
/// </summary>
public sealed record CreateExperienceReviewCommand(Guid ExperienceBookingId, int Rating, string Comment) : ICommand<Guid>;

internal sealed class CreateExperienceReviewCommandHandler(
    IUserIdentifierProvider userIdentifierProvider,
    IExperienceRepository experienceRepository,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<CreateExperienceReviewCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateExperienceReviewCommand request, CancellationToken cancellationToken)
    {
        ExperienceBooking? booking = await experienceRepository.GetBookingAsync(request.ExperienceBookingId, cancellationToken);

        if (booking is null || booking.GuestId != userIdentifierProvider.UserId)
        {
            return Result.Failure<Guid>(ExperienceBookingErrors.NotFound(request.ExperienceBookingId));
        }

        if (await experienceRepository.HasReviewAsync(booking.Id, cancellationToken))
        {
            return Result.Failure<Guid>(ExperienceBookingErrors.AlreadyReviewed);
        }

        Result<ExperienceReview> review = ExperienceReview.Create(
            booking,
            request.Rating,
            request.Comment,
            dateTimeProvider.UtcNow);

        if (review.IsFailure)
        {
            return Result.Failure<Guid>(review.Error);
        }

        experienceRepository.InsertReview(review.Value);

        return review.Value.Id;
    }
}
