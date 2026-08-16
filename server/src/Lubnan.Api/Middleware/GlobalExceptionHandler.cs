using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Lubnan.Api.Middleware;

/// <summary>
/// The last line of defence. Anything that escapes a handler leaves as
/// <c>ProblemDetails</c>, in the same shape as every expected failure.
/// </summary>
/// <remarks>
/// Two things it deliberately does not do. It does not send the exception
/// message to the client — a message can carry a connection string, a file
/// path or a row of somebody else's data, and "unexpected error" plus a
/// correlation id is both safer and more useful. And it does not swallow: the
/// exception is logged in full, with the same id the client was given, so the
/// two can be joined.
/// </remarks>
internal sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var correlationId = httpContext.Items[CorrelationIdMiddleware.ItemKey] as string
                            ?? httpContext.TraceIdentifier;

        logger.Unhandled(exception, httpContext.Request.Method, httpContext.Request.Path, correlationId);

        var problem = new ProblemDetails
        {
            Type = "https://lubnan.app/errors/failure",
            Title = "Something went wrong on our side.",
            Status = StatusCodes.Status500InternalServerError,
            Instance = $"{httpContext.Request.Method} {httpContext.Request.Path}",
            Extensions =
            {
                ["code"] = "server.unexpected",
                ["correlationId"] = correlationId,
                ["traceId"] = System.Diagnostics.Activity.Current?.Id ?? httpContext.TraceIdentifier,
            },
        };

        httpContext.Response.StatusCode = problem.Status.Value;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken).ConfigureAwait(false);

        return true;
    }
}

internal static partial class GlobalExceptionHandlerMessages
{
    [LoggerMessage(
        EventId = 3000,
        Level = LogLevel.Error,
        Message = "Unhandled exception on {Method} {Path} [{CorrelationId}]")]
    public static partial void Unhandled(
        this ILogger logger, Exception exception, string method, string path, string correlationId);
}
