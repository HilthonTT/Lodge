using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Experiences;
using Lodge.Domain.Users;

namespace Lodge.Application.Experiences.Reserve;

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
