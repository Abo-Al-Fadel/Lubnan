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

        /*
         * A body the framework could not read is the caller's mistake, and it
         * has to leave as one.
         *
         * Minimal APIs raise BadHttpRequestException when a JSON body will not
         * parse or a field arrives as the wrong type, and that exception
         * already carries the status it wants — 400. Handling every exception
         * identically overrode it with 500, so `{"email": 12345}` came back as
         * "Something went wrong on our side": the client was told to retry
         * something that will never succeed, and the server logged its own
         * fault for somebody else's typo.
         *
         * The cost was not only cosmetic. Every scanner and every mistyped
         * curl became an Error-level log line and a Sentry event, which is the
         * noise a real 500 has to be found underneath. The Sentry filter in
         * Program.cs suppresses exactly this exception type — evidence the
         * symptom was noticed there while the status stayed wrong here.
         *
         * The message is still not forwarded. It names the parameter and the
         * DTO ("Failed to read parameter \"LoginRequest body\"…"), which is
         * internal shape the caller has no business learning.
         */
        if (exception is BadHttpRequestException malformed)
        {
            // Guarded, unlike the Error-level call below, and CA1873 is right to
            // insist. Request.Path is a PathString, so passing it converts to a
            // string at the call site - before the generated log method checks
            // whether anything is listening. At Error that check all but always
            // passes and the conversion is not wasted; at Information it can be
            // disabled in a deployment, and then this allocates on every
            // malformed request for nobody.
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.Malformed(httpContext.Request.Method, httpContext.Request.Path, correlationId);
            }

            await WriteAsync(
                httpContext,
                new ProblemDetails
                {
                    Type = "https://lubnan.app/errors/validation",
                    Title = "That request could not be read.",
                    Status = malformed.StatusCode,
                    Instance = $"{httpContext.Request.Method} {httpContext.Request.Path}",
                    Extensions =
                    {
                        ["code"] = "request.malformed",
                        ["correlationId"] = correlationId,
                    },
                },
                cancellationToken).ConfigureAwait(false);

            return true;
        }

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

        await WriteAsync(httpContext, problem, cancellationToken).ConfigureAwait(false);

        return true;
    }

    private static async Task WriteAsync(
        HttpContext httpContext, ProblemDetails problem, CancellationToken cancellationToken)
    {
        httpContext.Response.StatusCode = problem.Status!.Value;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken).ConfigureAwait(false);
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

    // Information, not Error, and without the exception. A body that will not
    // parse says something about the caller and nothing about this service, so
    // it belongs in the access record rather than in the list of things to go
    // and fix.
    [LoggerMessage(
        EventId = 3001,
        Level = LogLevel.Information,
        Message = "Unreadable request body on {Method} {Path} [{CorrelationId}]")]
    public static partial void Malformed(
        this ILogger logger, string method, string path, string correlationId);
}
