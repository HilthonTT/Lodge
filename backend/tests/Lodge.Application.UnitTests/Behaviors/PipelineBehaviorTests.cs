using Lodge.Application.Abstractions.Behaviors;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Idempotency;
using Lodge.Application.Abstractions.Realtime;
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
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2030, 1, 1), new DateOnly(2030, 1, 3), GuestCount: 2);

    [Theory]
    [InlineData(IdempotencyStatus.InProgress, "Request.InProgress")]
    [InlineData(IdempotencyStatus.KeyReused, "Request.KeyReused")]
    public async Task Idempotency_Should_ReturnTypedConflict_WithoutRunningTheCommand(IdempotencyStatus status, string code)
    {
        // Regression: the behavior used to cast Result to Result<Guid>, which threw InvalidCastException.
        IIdempotencyService idempotencyService = IdempotencyService(new IdempotencyCheck(status));

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
        result.Error.Code.Should().Be(code);
        result.Error.Type.Should().Be(ErrorType.Conflict);
        handlerCalled.Should().BeFalse();
        idempotencyService.DidNotReceiveWithAnyArgs().Complete(default, default!, default!);
    }

    [Fact]
    public async Task Idempotency_Should_StoreTheResult_AndReplayIt()
    {
        Guid bookingId = Guid.NewGuid();
        IIdempotencyService first = IdempotencyService(IdempotencyCheck.New);
        string? stored = null;
        first.When(s => s.Complete(Command.RequestId, nameof(ReserveBookingCommand), Arg.Any<string>()))
            .Do(call => stored = call.ArgAt<string>(2));

        Result<Guid> original = await new IdempotentCommandPipelineBehavior<ReserveBookingCommand, Result<Guid>>(first)
            .Handle(Command, _ => Task.FromResult(Result.Success(bookingId)), default);

        original.Value.Should().Be(bookingId);
        stored.Should().NotBeNull();

        bool handlerCalled = false;
        Result<Guid> replay = await new IdempotentCommandPipelineBehavior<ReserveBookingCommand, Result<Guid>>(
                IdempotencyService(new IdempotencyCheck(IdempotencyStatus.Completed, stored)))
            .Handle(
                Command,
                _ =>
                {
                    handlerCalled = true;
                    return Task.FromResult(Result.Success(Guid.NewGuid()));
                },
                default);

        replay.IsSuccess.Should().BeTrue();
        replay.Value.Should().Be(bookingId);
        handlerCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Idempotency_Should_NotStoreTheKey_WhenTheCommandFails()
    {
        IIdempotencyService idempotencyService = IdempotencyService(IdempotencyCheck.New);

        Result<Guid> result = await new IdempotentCommandPipelineBehavior<ReserveBookingCommand, Result<Guid>>(idempotencyService)
            .Handle(Command, _ => Task.FromResult(Result.Failure<Guid>(Error.Problem("Test.Failure", "failure"))), default);

        result.IsFailure.Should().BeTrue();
        idempotencyService.DidNotReceiveWithAnyArgs().Complete(default, default!, default!);
    }

    [Fact]
    public async Task Idempotency_Should_RejectLegacyKeysWithoutAStoredResult()
    {
        Result<Guid> result = await new IdempotentCommandPipelineBehavior<ReserveBookingCommand, Result<Guid>>(
                IdempotencyService(new IdempotencyCheck(IdempotencyStatus.Completed)))
            .Handle(Command, _ => Task.FromResult(Result.Success(Guid.NewGuid())), default);

        result.Error.Should().Be(IdempotencyErrors.AlreadyProcessed);
    }

    [Fact]
    public void ResultFactory_Should_RoundTripSuccessfulResults()
    {
        var value = new CheckoutLike(Guid.NewGuid(), "https://example.test");

        ResultFactory.Deserialize<Result<CheckoutLike>>(ResultFactory.Serialize(Result.Success(value)))
            .Value.Should().Be(value);
        ResultFactory.Deserialize<Result>(ResultFactory.Serialize(Result.Success())).IsSuccess.Should().BeTrue();
    }

    private static IIdempotencyService IdempotencyService(IdempotencyCheck check)
    {
        IIdempotencyService idempotencyService = Substitute.For<IIdempotencyService>();
        idempotencyService
            .BeginAsync(Command.RequestId, nameof(ReserveBookingCommand), Arg.Any<CancellationToken>())
            .Returns(check);

        return idempotencyService;
    }

    public sealed record CheckoutLike(Guid Id, string Url);

    [Fact]
    public async Task UnitOfWork_Should_RollBack_And_NotSave_WhenCommandFails()
    {
        (IUnitOfWork unitOfWork, IDbContextTransaction transaction) = CreateUnitOfWork();
        var behavior = new UnitOfWorkPipelineBehavior<ReserveBookingCommand, Result<Guid>>(unitOfWork, Substitute.For<IRealtimeOutbox>());

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
        var behavior = new UnitOfWorkPipelineBehavior<ReserveBookingCommand, Result<Guid>>(unitOfWork, Substitute.For<IRealtimeOutbox>());

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
