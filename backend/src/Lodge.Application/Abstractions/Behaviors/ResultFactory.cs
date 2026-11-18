using System.Collections.Concurrent;
using System.Reflection;
using Lodge.Domain.Core.Primitives;

namespace Lodge.Application.Abstractions.Behaviors;

/// <summary>
/// Creates failed <see cref="Result"/> / <see cref="Result{TValue}"/> instances for pipeline behaviors
/// that only know the response type through a generic argument.
/// </summary>
internal static class ResultFactory
{
    private static readonly MethodInfo GenericFailureMethod = typeof(Result)
        .GetMethods(BindingFlags.Public | BindingFlags.Static)
        .Single(m => m is { Name: nameof(Result.Failure), IsGenericMethodDefinition: true });

    private static readonly ConcurrentDictionary<Type, Func<Error, object>> Factories = new();

    public static bool CanCreate(Type responseType) =>
        responseType == typeof(Result) ||
        (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>));

    public static TResponse Failure<TResponse>(Error error)
    {
        Func<Error, object> factory = Factories.GetOrAdd(typeof(TResponse), CreateFactory);

        return (TResponse)factory(error);
    }

    private static Func<Error, object> CreateFactory(Type responseType)
    {
        if (responseType == typeof(Result))
        {
            return Result.Failure;
        }

        if (!CanCreate(responseType))
        {
            throw new InvalidOperationException($"Cannot create a failure result for '{responseType}'.");
        }

        MethodInfo method = GenericFailureMethod.MakeGenericMethod(responseType.GetGenericArguments()[0]);

        return error => method.Invoke(null, [error])!;
    }
}
