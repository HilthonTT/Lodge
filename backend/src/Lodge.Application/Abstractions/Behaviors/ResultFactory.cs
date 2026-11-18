using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using Lodge.Domain.Core.Primitives;

namespace Lodge.Application.Abstractions.Behaviors;

/// <summary>
/// Creates <see cref="Result"/> / <see cref="Result{TValue}"/> instances for pipeline behaviors that only know the
/// response type through a generic argument, and (de)serializes successful results for idempotent replays.
/// </summary>
internal static class ResultFactory
{
    private static readonly JsonSerializerOptions SerializerOptions = JsonSerializerOptions.Web;

    private static readonly MethodInfo GenericFailureMethod = typeof(Result)
        .GetMethods(BindingFlags.Public | BindingFlags.Static)
        .Single(m => m is { Name: nameof(Result.Failure), IsGenericMethodDefinition: true });

    private static readonly MethodInfo GenericSuccessMethod = typeof(Result)
        .GetMethods(BindingFlags.Public | BindingFlags.Static)
        .Single(m => m is { Name: nameof(Result.Success), IsGenericMethodDefinition: true });

    private static readonly ConcurrentDictionary<Type, Func<Error, object>> Factories = new();

    private static readonly ConcurrentDictionary<Type, Func<object?, object>> SuccessFactories = new();

    public static bool CanCreate(Type responseType) =>
        responseType == typeof(Result) ||
        (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>));

    public static TResponse Failure<TResponse>(Error error)
    {
        Func<Error, object> factory = Factories.GetOrAdd(typeof(TResponse), CreateFactory);

        return (TResponse)factory(error);
    }

    /// <summary>
    /// Serializes the value of a successful result as JSON (<c>null</c> for a plain <see cref="Result"/>).
    /// </summary>
    public static string Serialize(Result result)
    {
        ArgumentNullException.ThrowIfNull(result);

        Type? valueType = GetValueType(result.GetType());

        if (valueType is null)
        {
            return "null";
        }

        object? value = result.GetType().GetProperty(nameof(Result<object>.Value))!.GetValue(result);

        return JsonSerializer.Serialize(value, valueType, SerializerOptions);
    }

    /// <summary>
    /// Recreates a successful result from the JSON written by <see cref="Serialize"/>.
    /// </summary>
    public static TResponse Deserialize<TResponse>(string json)
    {
        Type? valueType = GetValueType(typeof(TResponse));

        if (valueType is null)
        {
            return (TResponse)(object)Result.Success();
        }

        object? value = JsonSerializer.Deserialize(json, valueType, SerializerOptions);

        Func<object?, object> factory = SuccessFactories.GetOrAdd(valueType, type =>
        {
            MethodInfo method = GenericSuccessMethod.MakeGenericMethod(type);

            return v => method.Invoke(null, [v])!;
        });

        return (TResponse)factory(value);
    }

    private static Type? GetValueType(Type responseType)
    {
        for (Type? type = responseType; type is not null; type = type.BaseType)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Result<>))
            {
                return type.GetGenericArguments()[0];
            }
        }

        return null;
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
