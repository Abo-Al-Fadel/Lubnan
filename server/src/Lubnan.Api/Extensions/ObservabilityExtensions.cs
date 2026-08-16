using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Lubnan.Api.Extensions;

/// <summary>
/// Traces, metrics and logs, exported over OTLP.
/// </summary>
/// <remarks>
/// Wired from the first commit rather than added after the first incident.
/// Retrofitting observability means guessing which spans you wish you had while
/// the thing you are trying to understand is no longer happening.
/// <para>
/// OTLP rather than a vendor SDK, so the collector on the other end can be Seq,
/// Jaeger, Grafana or a hosted product without a code change. If no endpoint is
/// configured the exporter is simply not registered, so local development does
/// not need a collector running.
/// </para>
/// </remarks>
public static class ObservabilityExtensions
{
    public const string ServiceName = "lubnan-api";

    public static IServiceCollection AddObservability(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var otlpEndpoint = configuration["OpenTelemetry:OtlpEndpoint"];

        var otel = services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(
                serviceName: ServiceName,
                serviceVersion: typeof(ObservabilityExtensions).Assembly.GetName().Version?.ToString() ?? "0.0.0"))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation(options =>
                {
                    // Health probes run every few seconds forever. Tracing them
                    // buys nothing and drowns the traces that matter.
                    options.Filter = context => !context.Request.Path.StartsWithSegments("/health");
                })
                .AddHttpClientInstrumentation()

                // Npgsql publishes its own ActivitySource. Subscribing by name
                // gets query spans without a package that would pull in a
                // second major version of the driver.
                .AddSource("Npgsql"))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation());

        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            otel.UseOtlpExporter();
        }

        return services;
    }
}
