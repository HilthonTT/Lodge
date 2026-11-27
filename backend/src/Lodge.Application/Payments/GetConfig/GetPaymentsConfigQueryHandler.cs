using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Payments;
using Lodge.Domain.Core.Primitives;

namespace Lodge.Application.Payments.GetConfig;

internal sealed class GetPaymentsConfigQueryHandler(IPaymentModeProvider paymentModeProvider)
    : IQueryHandler<GetPaymentsConfigQuery, PaymentsConfigResponse>
{
    public Task<Result<PaymentsConfigResponse>> Handle(GetPaymentsConfigQuery request, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success(new PaymentsConfigResponse(paymentModeProvider.Mode.ToString())));
}
