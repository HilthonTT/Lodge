using System.Data;
using Dapper;
using FluentValidation;
using Lodge.Application.Abstractions.App;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;
using Lodge.Application.Hosting;
using Lodge.Application.Payments;
using Lodge.Application.Stripe;
using Lodge.Contracts.Common;
using Lodge.Contracts.Payments;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Payouts;
using Lodge.Domain.Users;
using Microsoft.Extensions.Options;

namespace Lodge.Application.Payouts;

/// <summary>
/// Gets the caller's payout account (<c>GET /hosting/payouts/account</c>).
/// </summary>
public sealed record GetPayoutAccountQuery : IQuery<PayoutAccountResponse>;

internal sealed class GetPayoutAccountQueryHandler(
    IHostAccess hostAccess,
    IPayoutRepository payoutRepository,
    IPaymentModeProvider paymentModeProvider) : IQueryHandler<GetPayoutAccountQuery, PayoutAccountResponse>
{
    public async Task<Result<PayoutAccountResponse>> Handle(GetPayoutAccountQuery request, CancellationToken cancellationToken)
    {
        Result<HostCaller> caller = await hostAccess.GetCallerAsync(cancellationToken);

        if (caller.IsFailure)
        {
            return Result.Failure<PayoutAccountResponse>(caller.Error);
        }

        PayoutAccount? account = await payoutRepository.GetAccountByUserIdAsync(caller.Value.UserId, cancellationToken);

        return new PayoutAccountResponse(
            (account?.Status ?? PayoutAccountStatus.NotConnected).ToString(),
            account?.PayoutsEnabled ?? false,
            paymentModeProvider.Mode.ToString());
    }
}

/// <summary>
/// Starts (or resumes) the payout onboarding of the caller (<c>POST /hosting/payouts/onboarding</c>): a Stripe Connect
/// Express account link, or in simulated mode an immediately active account and the frontend's payouts page.
/// </summary>
public sealed record StartPayoutOnboardingCommand : ICommand<UrlResponse>;

internal sealed class StartPayoutOnboardingCommandHandler(
    IHostAccess hostAccess,
    IUserRepository userRepository,
    IPayoutRepository payoutRepository,
    PaymentGateways paymentGateways,
    StripeClientProvider stripeClientProvider,
    IOptions<AppOptions> appOptions) : ICommandHandler<StartPayoutOnboardingCommand, UrlResponse>
{
    public async Task<Result<UrlResponse>> Handle(StartPayoutOnboardingCommand request, CancellationToken cancellationToken)
    {
        Result<HostCaller> caller = await hostAccess.GetCallerAsync(cancellationToken);

        if (caller.IsFailure)
        {
            return Result.Failure<UrlResponse>(caller.Error);
        }

        bool simulated = paymentGateways.Mode == PaymentMode.Simulated;

        if (!simulated && !stripeClientProvider.IsConfigured)
        {
            return Result.Failure<UrlResponse>(PaymentErrors.PayoutAccountNotConfigured);
        }

        string returnUrl = appOptions.Value.FrontendUrl("hosting/payouts?onboarding=complete");
        string refreshUrl = appOptions.Value.FrontendUrl("hosting/payouts?onboarding=refresh");

        PayoutAccount? account = await payoutRepository.GetAccountByUserIdAsync(caller.Value.UserId, cancellationToken);

        if (account is null)
        {
            User user = (await userRepository.GetByIdAsync(caller.Value.UserId, cancellationToken))!;

            string accountId = await paymentGateways.Current.CreateConnectedAccountAsync(user.Email.Value, cancellationToken);

            account = PayoutAccount.Create(user.Id, accountId);
            payoutRepository.InsertAccount(account);
        }

        if (SimulatedIds.IsSimulated(account.ExternalAccountId))
        {
            // Simulated onboarding completes immediately; payouts on hold are retried (PayoutAccountActivated event).
            account.Update(payoutsEnabled: true);

            return new UrlResponse(returnUrl);
        }

        string url = await paymentGateways.For(account.ExternalAccountId).CreateOnboardingLinkAsync(
            account.ExternalAccountId,
            refreshUrl,
            returnUrl,
            cancellationToken);

        return new UrlResponse(url);
    }
}

/// <summary>
/// Gets one page of the caller's payouts, newest first (<c>GET /hosting/payouts</c>).
/// </summary>
/// <param name="Status">An optional status filter (<c>Scheduled</c>, <c>OnHold</c>, <c>Paid</c>, <c>Failed</c>, <c>Cancelled</c>).</param>
/// <param name="Page">The page.</param>
/// <param name="PageSize">The page size.</param>
public sealed record GetPayoutsQuery(string? Status, int Page, int PageSize) : IQuery<PagedList<PayoutResponse>>;

internal sealed class GetPayoutsQueryValidator : AbstractValidator<GetPayoutsQuery>
{
    public GetPayoutsQueryValidator()
    {
        RuleFor(x => x.Page).ValidPage();
        RuleFor(x => x.PageSize).ValidPageSize();

        RuleFor(x => x.Status)
            .Must(status => string.IsNullOrWhiteSpace(status) || EnumNames.TryParse<PayoutStatus>(status, out _))
            .WithError(ValidationErrors.Payouts.InvalidStatus);
    }
}

internal sealed class GetPayoutsQueryHandler(IHostAccess hostAccess, IDbConnectionFactory factory)
    : IQueryHandler<GetPayoutsQuery, PagedList<PayoutResponse>>
{
    public async Task<Result<PagedList<PayoutResponse>>> Handle(GetPayoutsQuery request, CancellationToken cancellationToken)
    {
        Result<HostCaller> caller = await hostAccess.GetCallerAsync(cancellationToken);

        if (caller.IsFailure)
        {
            return Result.Failure<PagedList<PayoutResponse>>(caller.Error);
        }

        string? status = string.IsNullOrWhiteSpace(request.Status)
            ? null
            : EnumNames.Parse<PayoutStatus>(request.Status).ToString();

        string where = status is null ? "p.host_id = @HostId" : "p.host_id = @HostId AND p.status = @Status";

        string sql =
            $"""
            SELECT COUNT(*) FROM payouts AS p WHERE {where};

            SELECT
                p.id AS Id,
                p.booking_id AS BookingId,
                p.experience_booking_id AS ExperienceBookingId,
                COALESCE(a.name, e.title, '') AS ListingName,
                COALESCE(u.first_name || ' ' || u.last_name, eu.first_name || ' ' || eu.last_name, '') AS GuestName,
                p.amount_amount AS Amount,
                p.amount_currency AS Currency,
                p.status AS Status,
                p.scheduled_for::timestamp AS ScheduledFor,
                p.paid_on_utc AS PaidOnUtc
            FROM payouts AS p
            LEFT JOIN bookings AS b ON b.id = p.booking_id
            LEFT JOIN apartments AS a ON a.id = b.apartment_id
            LEFT JOIN users AS u ON u.id = b.user_id
            LEFT JOIN experience_bookings AS eb ON eb.id = p.experience_booking_id
            LEFT JOIN experiences AS e ON e.id = eb.experience_id
            LEFT JOIN users AS eu ON eu.id = eb.guest_id
            WHERE {where}
            ORDER BY p.scheduled_for DESC, p.id
            LIMIT @PageSize OFFSET @Offset;
            """;

        using IDbConnection connection = await factory.GetOpenConnectionAsync(cancellationToken);

        using SqlMapper.GridReader grid = await connection.QueryMultipleAsync(
            sql,
            new
            {
                HostId = caller.Value.UserId,
                Status = status ?? string.Empty,
                request.PageSize,
                Offset = (request.Page - 1) * request.PageSize,
            });

        int totalCount = await grid.ReadSingleAsync<int>();

        List<PayoutResponse> items =
        [
            .. (await grid.ReadAsync<PayoutRow>()).Select(row => new PayoutResponse
            {
                Id = row.Id,
                BookingId = row.BookingId,
                ExperienceBookingId = row.ExperienceBookingId,
                ListingName = row.ListingName,
                GuestName = row.GuestName,
                Amount = row.Amount,
                Currency = row.Currency,
                Status = row.Status,
                ScheduledFor = DateOnly.FromDateTime(row.ScheduledFor),
                PaidOnUtc = row.PaidOnUtc,
            }),
        ];

        return new PagedList<PayoutResponse>(items, request.Page, request.PageSize, totalCount);
    }

    private sealed class PayoutRow
    {
        public Guid Id { get; init; }

        public Guid? BookingId { get; init; }

        public Guid? ExperienceBookingId { get; init; }

        public string ListingName { get; init; } = string.Empty;

        public string GuestName { get; init; } = string.Empty;

        public decimal Amount { get; init; }

        public string Currency { get; init; } = string.Empty;

        public string Status { get; init; } = string.Empty;

        public DateTime ScheduledFor { get; init; }

        public DateTime? PaidOnUtc { get; init; }
    }
}
