using System.Reflection;
using FluentValidation;
using Lubnan.Application.Abstractions.Messaging;
using Lubnan.Application.Behaviors;
using Microsoft.Extensions.DependencyInjection;

namespace Lubnan.Application;

/// <summary>Everything this assembly contributes to the container.</summary>
public static class DependencyInjection
{
    /// <summary>Marker for assembly scanning; never instantiated.</summary>
    public static readonly Assembly Assembly = typeof(DependencyInjection).Assembly;

    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ISender, Sender>();

        services.AddRequestHandlersFrom(Assembly);
        services.AddValidatorsFromAssembly(Assembly, includeInternalTypes: true);

        // Outermost first. Logging wraps validation so a rejected request is
        // still one line in the log with its outcome, rather than a silence
        // that looks like the request never arrived.
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }

    /// <summary>
    /// Registers every <see cref="IRequestHandler{TRequest,TResponse}"/> in an
    /// assembly against the closed interface it implements.
    /// </summary>
    /// <remarks>
    /// Scanning rather than a list, because a list is a file two people edit on
    /// every branch, and a handler missing from it fails at runtime on the one
    /// path nobody exercised. Scanning fails at startup instead — and
    /// <c>Lubnan.Architecture.Tests</c> fails earlier still, at build time, if a
    /// request has no handler or has two.
    /// </remarks>
    private static void AddRequestHandlersFrom(this IServiceCollection services, Assembly assembly)
    {
        foreach (var type in assembly.GetTypes().Where(t => t is { IsAbstract: false, IsInterface: false }))
        {
            foreach (var contract in type.GetInterfaces().Where(IsRequestHandler))
            {
                services.AddScoped(contract, type);
            }
        }
    }

    private static bool IsRequestHandler(Type contract) =>
        contract.IsGenericType
        && contract.GetGenericTypeDefinition() == typeof(IRequestHandler<,>);
}
