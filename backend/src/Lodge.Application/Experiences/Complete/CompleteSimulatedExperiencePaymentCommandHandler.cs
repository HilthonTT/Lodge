using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Application.Core.Errors;
using Lodge.Application.Payments;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Experiences;

namespace Lodge.Application.Experiences.Complete;

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
