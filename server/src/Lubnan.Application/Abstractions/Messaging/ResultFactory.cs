using System.Collections.Concurrent;
using System.Reflection;
using Lubnan.Domain.Common;

namespace Lubnan.Application.Abstractions.Messaging;

/// <summary>
/// Builds a failed <see cref="Result"/> or <c>Result&lt;T&gt;</c> when only the
/// closed response type is known, which is the position every pipeline
/// behaviour is in.
/// </summary>
/// <remarks>
/// The alternative is for a behaviour to throw and for the exception handler to
/// turn it back into a response. That works, but it makes an ordinary rejected
/// form into an exception — with the stack capture and the noise in the error
/// dashboard that implies — on a path that runs constantly. A short-circuit
/// should look like a return.
/// <para>
/// One reflection lookup per response type, cached as a delegate. After the
/// first call it is a dictionary hit and an invocation.
/// </para>
/// </remarks>
internal static class ResultFactory
{
    private static readonly ConcurrentDictionary<Type, Func<Error, Result>> Factories = new();

    public static TResponse Failure<TResponse>(Error error)
        where TResponse : Result
    {
        if (typeof(TResponse) == typeof(Result))
        {
            return (TResponse)Result.Failure(error);
        }

        var factory = Factories.GetOrAdd(typeof(TResponse), static responseType =>
        {
            var valueType = responseType.GetGenericArguments()[0];

            var method = typeof(Result)
                .GetMethod(nameof(Result.Failure), 1, BindingFlags.Public | BindingFlags.Static, null, [typeof(Error)], null)!
                .MakeGenericMethod(valueType);

            return method.CreateDelegate<Func<Error, Result>>();
        });

        return (TResponse)factory(error);
    }
}
