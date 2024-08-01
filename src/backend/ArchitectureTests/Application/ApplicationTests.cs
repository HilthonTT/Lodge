using FluentAssertions;
using FluentValidation;
using Lodge.Application.Abstractions.Messaging;
using NetArchTest.Rules;

namespace ArchitectureTests.Application;

public class ApplicationTests : BaseTest
{
    [Fact]
    public void CommandHandlers_ShouldHave_NameEndingWith_CommandHandler()
    {
        TestResult result = Types.InAssembly(ApplicationAssembly)
            .That()
            .ImplementInterface(typeof(ICommandHandler<>))
            .Or()
            .ImplementInterface(typeof(ICommandHandler<>))
            .Should()
            .HaveNameEndingWith("CommandHandler")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void CommandHandlers_Should_NotBePublic()
    {
        TestResult result = Types.InAssembly(ApplicationAssembly)
           .That()
           .ImplementInterface(typeof(ICommandHandler<>))
           .Or()
           .ImplementInterface(typeof(ICommandHandler<,>))
           .Should()
           .NotBePublic()
           .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void CommandHandlers_Should_BeSealed()
    {
        TestResult result = Types.InAssembly(ApplicationAssembly)
           .That()
           .ImplementInterface(typeof(ICommandHandler<>))
           .Or()
           .ImplementInterface(typeof(ICommandHandler<,>))
           .Should()
           .BeSealed()
           .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void QueryHandlers_ShouldHave_NameEndingWith_QueryHandler()
    {
        TestResult result = Types.InAssembly(ApplicationAssembly)
           .That()
           .ImplementInterface(typeof(IQueryHandler<,>))
           .Should()
           .HaveNameEndingWith("QueryHandler")
           .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void QueryHandlers_Should_NotBePublic()
    {
        TestResult result = Types.InAssembly(ApplicationAssembly)
           .That()
           .ImplementInterface(typeof(IQueryHandler<,>))
           .Should()
           .NotBePublic()
           .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void QueryHandlers_Should_BeSealed()
    {
        TestResult result = Types.InAssembly(ApplicationAssembly)
           .That()
           .ImplementInterface(typeof(IQueryHandler<,>))
           .Should()
           .BeSealed()
           .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Validators_ShouldHave_NameEndingWith_Validator()
    {
        TestResult result = Types.InAssembly(ApplicationAssembly)
           .That()
           .Inherit(typeof(AbstractValidator<>))
           .Should()
           .HaveNameEndingWith("Validator")
           .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Validators_Should_NotBePublic()
    {
        TestResult result = Types.InAssembly(ApplicationAssembly)
           .That()
           .Inherit(typeof(AbstractValidator<>))
           .Should()
           .NotBePublic()
           .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Validators_Should_BeSealed()
    {
        TestResult result = Types.InAssembly(ApplicationAssembly)
           .That()
           .Inherit(typeof(AbstractValidator<>))
           .Should()
           .BeSealed()
           .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }
}
