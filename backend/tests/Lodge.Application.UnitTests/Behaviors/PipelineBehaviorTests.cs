using Lodge.Application.Abstractions.Behaviors;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Idempotency;
using Lodge.Application.Bookings.Reserve;
using Lodge.Application.Core.Errors;
using Lodge.Domain.Core.Primitives;
using MediatR;
using Microsoft.EntityFrameworkCore.Storage;
using NSubstitute;

namespace Lodge.Application.UnitTests.Behaviors;

public class PipelineBehaviorTests
{
    private static readonly ReserveBookingCommand Command = new(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2030, 1, 1), new DateOnly(2030, 1, 3));

    [Fact]
    public async Task Idempotency_Should_ReturnTypedConflict_WhenRequestWasAlreadyProcessed()
    {
        // Regression: the behavior used to cast Result to Result<Guid>, which threw InvalidCastException.
        IIdempotencyService idempotencyService = Substitute.For<IIdempotencyService>();
        idempotencyService
            .TryCreateRequestAsync(Command.RequestId, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var behavior = new IdempotentCommandPipelineBehavior<ReserveBookingCommand, Result<Guid>>(idempotencyService);
        bool handlerCalled = false;

        Result<Guid> result = await behavior.Handle(
            Command,
            _ =>
            {
                handlerCalled = true;
                return Task.FromResult(Result.Success(Guid.NewGuid()));
            },
            default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(IdempotencyErrors.AlreadyProcessed);
        handlerCalled.Should().BeFalse();
    }

    [Fact]
    public async Task UnitOfWork_Should_RollBack_And_NotSave_WhenCommandFails()
    {
        (IUnitOfWork unitOfWork, IDbContextTransaction transaction) = CreateUnitOfWork();
        var behavior = new UnitOfWorkPipelineBehavior<ReserveBookingCommand, Result<Guid>>(unitOfWork);

        Result<Guid> result = await behavior.Handle(
            Command,
            _ => Task.FromResult(Result.Failure<Guid>(Error.Problem("Test.Failure", "failure"))),
            default);

        result.IsFailure.Should().BeTrue();
        await transaction.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
        await transaction.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        unitOfWork.Received(1).DiscardChanges();
    }

    [Fact]
    public async Task UnitOfWork_Should_SaveAndCommit_WhenCommandSucceeds()
    {
        (IUnitOfWork unitOfWork, IDbContextTransaction transaction) = CreateUnitOfWork();
        var behavior = new UnitOfWorkPipelineBehavior<ReserveBookingCommand, Result<Guid>>(unitOfWork);

        Result<Guid> result = await behavior.Handle(
            Command,
            _ => Task.FromResult(Result.Success(Guid.NewGuid())),
            default);

        result.IsSuccess.Should().BeTrue();
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await transaction.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void ResultFactory_Should_CreateFailures_ForBothResultTypes()
    {
        Error error = Error.Problem("Test.Failure", "failure");

        ResultFactory.Failure<Result>(error).Error.Should().Be(error);
        ResultFactory.Failure<Result<string>>(error).Error.Should().Be(error);
    }

    private static (IUnitOfWork UnitOfWork, IDbContextTransaction Transaction) CreateUnitOfWork()
    {
        IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
        IDbContextTransaction transaction = Substitute.For<IDbContextTransaction>();
        unitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>()).Returns(transaction);

        return (unitOfWork, transaction);
    }
}
