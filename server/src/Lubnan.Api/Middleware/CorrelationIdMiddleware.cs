using Microsoft.Extensions.Primitives;

namespace Lubnan.Api.Middleware;

/// <summary>
/// Gives every request an id, returns it in the response, and puts it on every
/// log line the request produces.
/// </summary>
/// <remarks>
/// This is what turns "the site was broken at about four" into a single trace.
/// The id is echoed in a header so the frontend can show it on an error page,
/// and a user reporting a problem can quote eight characters instead of
/// describing what they were doing.
/// <para>
/// An inbound id is honoured so a chain of services shares one, but it is
/// length-capped and stripped of control characters first: it is attacker-
/// controlled input that ends up in log files, and a newline in a log line is
/// how forged log entries get written.
/// </para>
/// </remarks>
internal sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-Id";

    /// <summary>
    /// Where the id lives for the rest of the request.
    /// </summary>
    /// <remarks>
    /// Items rather than the response header, because
    /// <c>UseExceptionHandler</c> clears the response before writing its
    /// problem document — headers included. Reading the header back from the
    /// exception handler returns an empty string, which is a correlation id
    /// that correlates nothing, and it looks like it is working right up until
    /// the request you actually need to trace.
    /// </remarks>
    public const string ItemKey = "CorrelationId";

    private const int MaxLength = 64;

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var correlationId = Sanitise(context.Request.Headers[HeaderName]) ?? context.TraceIdentifier;

        context.Items[ItemKey] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        // The header has to be re-applied after the response is cleared, which
        // is what happens on the way back out of the exception handler.
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await next(context).ConfigureAwait(false);
        }
    }

    private static string? Sanitise(StringValues header)
    {
        var value = header.ToString();

        if (string.IsNullOrWhiteSpace(value) || value.Length > MaxLength)
        {
            return null;
        }

        // Conservative on purpose. Anything that is not an id shape is not an
        // id, and rejecting is cheaper than escaping at every point of use.
        return value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_') ? value : null;
    }
}
