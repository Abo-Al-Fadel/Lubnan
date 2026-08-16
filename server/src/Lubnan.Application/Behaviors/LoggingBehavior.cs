using System.Diagnostics;
using Lubnan.Application.Abstractions.Messaging;
using Lubnan.Domain.Common;
using Microsoft.Extensions.Logging;

namespace Lubnan.Application.Behaviors;

/// <summary>
/// One log line per request, carrying the outcome and how long it took.
/// </summary>
/// <remarks>
/// Structured, not interpolated: the request name is a field, so
/// "p99 latency of GetPlaceBySlug" is a query rather than a regular expression
/// over log text. Failures log the error <em>code</em>, never the message,
/// because the message is prose that will be translated and grouping on it
/// would split one problem across three languages.
/// <para>
/// Nothing here logs the request body. A command carries user input, and user
/// input includes things that must not be written to disk.
/// </para>
/// </remarks>
internal sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    private static readonly string RequestName = typeof(TRequest).Name;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();

        try
        {
            var response = await next().ConfigureAwait(false);
            var elapsed = Stopwatch.GetElapsedTime(started).TotalMilliseconds;

            if (response.IsSuccess)
            {
                logger.Succeeded(RequestName, elapsed);
            }
            else
            {
                // Expected failure. Information, not error: a 404 for a slug
                // nobody published is the system working, and paging somebody
                // for it trains them to ignore the alert.
                logger.Failed(RequestName, response.Error.Code, response.Error.Type, elapsed);
            }

            return response;
        }
        catch (Exception exception)
        {
            logger.Threw(exception, RequestName, Stopwatch.GetElapsedTime(started).TotalMilliseconds);
            throw;
        }
    }
}

/// <summary>
/// Source-generated log methods. Compiled delegates with no boxing and no
/// format parsing per call, which matters because this runs on every request.
/// </summary>
internal static partial class LoggingBehaviorMessages
{
    [LoggerMessage(
        EventId = 1000,
        Level = LogLevel.Information,
        Message = "{RequestName} succeeded in {ElapsedMs:0.0}ms")]
    public static partial void Succeeded(this ILogger logger, string requestName, double elapsedMs);

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "{RequestName} failed with {ErrorCode} ({ErrorType}) in {ElapsedMs:0.0}ms")]
    public static partial void Failed(
        this ILogger logger, string requestName, string errorCode, ErrorType errorType, double elapsedMs);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Error,
        Message = "{RequestName} threw after {ElapsedMs:0.0}ms")]
    public static partial void Threw(
        this ILogger logger, Exception exception, string requestName, double elapsedMs);
}
