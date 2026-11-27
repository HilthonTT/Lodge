using Lodge.Application.Abstractions.App;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Application.Core.Errors;
using Lodge.Application.Hosting;
using Lodge.Application.Payments;
using Lodge.Application.Stripe;
using Lodge.Contracts.Payments;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Payouts;
using Lodge.Domain.Users;
using Microsoft.Extensions.Options;

namespace Lodge.Application.Payouts.StartOnboarding;

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
