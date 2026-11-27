using Lodge.Application.Abstractions.Messaging;
using Lodge.Application.Hosting;
using Lodge.Application.Payments;
using Lodge.Contracts.Payments;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Payouts;

namespace Lodge.Application.Payouts.GetAccount;

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
