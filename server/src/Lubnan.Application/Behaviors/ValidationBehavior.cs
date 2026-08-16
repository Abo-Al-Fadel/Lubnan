using FluentValidation;
using Lubnan.Application.Abstractions.Messaging;
using Lubnan.Domain.Common;

namespace Lubnan.Application.Behaviors;

/// <summary>
/// Runs every validator registered for a request before its handler sees it.
/// </summary>
/// <remarks>
/// A handler in this codebase can therefore assume its input is structurally
/// sound and spend its lines on the actual decision. Adding a validator is
/// enough to enforce a rule — there is no second step where somebody remembers
/// to call it, which is the step that gets skipped on the twentieth feature.
/// <para>
/// Structural rules only. "Caption is at most 2000 characters" belongs here;
/// "this user has already reacted" belongs in the domain, because it depends
/// on state and a validator that reaches for state is a handler in disguise.
/// </para>
/// </remarks>
internal sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var applicable = validators as IValidator<TRequest>[] ?? validators.ToArray();

        if (applicable.Length == 0)
        {
            return await next().ConfigureAwait(false);
        }

        var context = new ValidationContext<TRequest>(request);

        var failures = (await Task.WhenAll(
                applicable.Select(v => v.ValidateAsync(context, cancellationToken)))
            .ConfigureAwait(false))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .Select(f => (f.PropertyName, f.ErrorMessage))
            .ToArray();

        return failures.Length == 0
            ? await next().ConfigureAwait(false)
            : ResultFactory.Failure<TResponse>(ValidationError.From(failures));
    }
}
