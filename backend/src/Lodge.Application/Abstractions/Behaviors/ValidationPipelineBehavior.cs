using FluentValidation;
using FluentValidation.Results;
using Lodge.Domain.Core.Primitives;
using MediatR;

namespace Lodge.Application.Abstractions.Behaviors;

/// <summary>
/// Runs the FluentValidation validators of a request and short-circuits with a <see cref="ValidationError"/>.
/// </summary>
internal sealed class ValidationPipelineBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : class
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        ValidationFailure[] validationFailures = await ValidateAsync(request, cancellationToken);

        if (validationFailures.Length == 0)
        {
            return await next(cancellationToken);
        }

        if (ResultFactory.CanCreate(typeof(TResponse)))
        {
            return ResultFactory.Failure<TResponse>(CreateValidationError(validationFailures));
        }

        throw new ValidationException(validationFailures);
    }

    private async Task<ValidationFailure[]> ValidateAsync(TRequest request, CancellationToken cancellationToken)
    {
        if (!validators.Any())
        {
            return [];
        }

        var context = new ValidationContext<TRequest>(request);

        ValidationResult[] validationResults = await Task.WhenAll(
            validators.Select(validator => validator.ValidateAsync(context, cancellationToken)));

        return validationResults
            .Where(validationResult => !validationResult.IsValid)
            .SelectMany(validationResult => validationResult.Errors)
            .ToArray();
    }

    private static ValidationError CreateValidationError(ValidationFailure[] validationFailures) =>
        new(validationFailures.Select(f => Error.Validation(f.ErrorCode, f.ErrorMessage)).ToArray());
}
