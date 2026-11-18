namespace Lodge.Application.Payments;

/// <summary>
/// Resolves the effective <see cref="PaymentMode"/> (see <see cref="PaymentsOptions.Mode"/>).
/// </summary>
public interface IPaymentModeProvider
{
    PaymentMode Mode { get; }
}
