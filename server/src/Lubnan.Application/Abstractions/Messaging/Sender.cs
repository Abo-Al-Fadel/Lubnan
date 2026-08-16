using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace Lubnan.Application.Abstractions.Messaging;

/// <summary>
/// Resolves the handler for a request and runs it through the registered
/// behaviours, outermost first.
/// </summary>
/// <remarks>
/// Hand-written rather than taken from a mediator library, for two reasons.
/// The first is licensing: the obvious library moved to a paid licence for
/// commercial use, and a dependency whose terms can change under you is a poor
/// place for the spine of an application. The second is that the whole thing
/// is sixty lines, and being able to explain exactly what happens between an
/// endpoint and a handler is worth more here than a package reference.
/// <para>
/// The per-request-type executor is built once and cached. Reflection happens
/// on the first call for a type and never again, so the steady-state cost is a
/// dictionary lookup and a virtual call.
/// </para>
/// </remarks>
internal sealed class Sender(IServiceProvider services) : ISender
{
    private static readonly ConcurrentDictionary<Type, object> Executors = new();

    public Task<TResponse> Send<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var executor = (Executor<TResponse>)Executors.GetOrAdd(
            request.GetType(),
            static requestType =>
            {
                var closed = typeof(Executor<,>).MakeGenericType(requestType, typeof(TResponse));
                return Activator.CreateInstance(closed)!;
            });

        return executor.Execute(services, request, cancellationToken);
    }

    /// <summary>Lets the cache hold one non-generic entry per request type.</summary>
    private abstract class Executor<TResponse>
    {
        public abstract Task<TResponse> Execute(
            IServiceProvider services,
            IRequest<TResponse> request,
            CancellationToken cancellationToken);
    }

    private sealed class Executor<TRequest, TResponse> : Executor<TResponse>
        where TRequest : IRequest<TResponse>
    {
        public override Task<TResponse> Execute(
            IServiceProvider services,
            IRequest<TResponse> request,
            CancellationToken cancellationToken)
        {
            var typed = (TRequest)request;
            var handler = services.GetRequiredService<IRequestHandler<TRequest, TResponse>>();

            RequestHandlerDelegate<TResponse> pipeline = () => handler.Handle(typed, cancellationToken);

            // Registration order is outermost-first as written in
            // DependencyInjection; wrapping in reverse produces that order.
            var behaviors = services.GetServices<IPipelineBehavior<TRequest, TResponse>>().ToArray();

            for (var i = behaviors.Length - 1; i >= 0; i--)
            {
                var behavior = behaviors[i];
                var next = pipeline;
                pipeline = () => behavior.Handle(typed, next, cancellationToken);
            }

            return pipeline();
        }
    }
}
