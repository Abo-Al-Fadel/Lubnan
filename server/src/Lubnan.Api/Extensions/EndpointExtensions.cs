using Lubnan.Application.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Lubnan.Api.Extensions;

/// <summary>Finds and maps every <see cref="IEndpoint"/> in an assembly.</summary>
public static class EndpointExtensions
{
    public static IServiceCollection AddEndpoints(
        this IServiceCollection services,
        System.Reflection.Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var endpoints = assembly
            .GetTypes()
            .Where(type => type is { IsAbstract: false, IsInterface: false, IsGenericTypeDefinition: false })
            .Where(type => type.IsAssignableTo(typeof(IEndpoint)))
            .Select(type => ServiceDescriptor.Transient(typeof(IEndpoint), type));

        services.TryAddEnumerableRange(endpoints);
        return services;
    }

    public static IApplicationBuilder MapEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        foreach (var endpoint in app.Services.GetRequiredService<IEnumerable<IEndpoint>>())
        {
            endpoint.Map(app);
        }

        return app;
    }

    private static void TryAddEnumerableRange(
        this IServiceCollection services,
        IEnumerable<ServiceDescriptor> descriptors)
    {
        foreach (var descriptor in descriptors)
        {
            services.Add(descriptor);
        }
    }
}
