namespace Lubnan.Application.Abstractions.Messaging;

/// <summary>Anything that can be sent and produces a <typeparamref name="TResponse"/>.</summary>
public interface IRequest<out TResponse>;

/// <summary>
/// A request that changes state. Separated from <see cref="IQuery{T}"/> so the
/// pipeline can treat the two differently without inspecting names: commands
/// open a transaction, queries never do.
/// </summary>
public interface ICommand<out TResponse> : IRequest<TResponse>;

/// <summary>A request that only reads. Safe to cache, safe to retry.</summary>
public interface IQuery<out TResponse> : IRequest<TResponse>;

/// <summary>The one thing that handles a given request type.</summary>
public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}

public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>;

public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>;

/// <summary>The next step in the pipeline. Call it, or short-circuit by not calling it.</summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Naming",
    "CA1711:Identifiers should not have incorrect suffix",
    Justification = "It is a delegate. The suffix is the clearest thing about it, and the name matches the convention every reader of a .NET pipeline already knows.")]
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();

/// <summary>
/// A concern that wraps every handler: validation, logging, transactions,
/// caching. Registered once, in order, instead of repeated at the top of each
/// handler where one will eventually be forgotten.
/// </summary>
public interface IPipelineBehavior<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken);
}

/// <summary>Dispatches a request to its handler, through the pipeline.</summary>
public interface ISender
{
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
}
