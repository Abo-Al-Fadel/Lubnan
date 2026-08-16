using Lubnan.Domain.Common;
using Microsoft.AspNetCore.Http;

namespace Lubnan.Application.Abstractions.Http;

/// <summary>
/// Turns a <see cref="Result"/> into an HTTP response. The single place in the
/// codebase where a domain failure becomes a status code.
/// </summary>
/// <remarks>
/// Because it is the only place, a new endpoint cannot invent its own
/// convention — no slice deciding that a missing row is a 200 with a null body
/// while its neighbour returns 404. Every failure leaves as RFC 7807
/// <c>ProblemDetails</c>, so a client writes one error path.
/// </remarks>
public static class ResultExtensions
{
    private const string ErrorDocsBase = "https://lubnan.app/errors/";

    public static IResult ToHttpResult(this Result result) =>
        result.IsSuccess ? Results.NoContent() : Problem(result.Error);

    public static IResult ToHttpResult<TValue>(this Result<TValue> result) =>
        result.IsSuccess ? Results.Ok(result.Value) : Problem(result.Error);

    /// <summary>For a POST that created something and should say where it is.</summary>
    public static IResult ToCreatedResult<TValue>(this Result<TValue> result, Func<TValue, string> location) =>
        result.IsSuccess
            ? Results.Created(location(result.Value), result.Value)
            : Problem(result.Error);

    private static IResult Problem(Error error)
    {
        var status = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError,
        };

        // The machine-readable half is the extension member "code". "type" is a
        // documentation URL and "title" is prose; neither is a stable contract,
        // and a client that branches on either will break on a copy edit.
        var extensions = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["code"] = error.Code,
        };

        if (error is ValidationError validation)
        {
            return Results.ValidationProblem(
                errors: validation.Failures.ToDictionary(f => f.Key, f => f.Value, StringComparer.Ordinal),
                title: error.Message,
                type: ErrorDocsBase + "validation",
                extensions: extensions);
        }

        return Results.Problem(
            title: error.Message,
            statusCode: status,
            type: ErrorDocsBase + Slugify(error.Type),
            extensions: extensions);
    }

    private static string Slugify(ErrorType type) => type switch
    {
        ErrorType.Validation => "validation",
        ErrorType.NotFound => "not-found",
        ErrorType.Conflict => "conflict",
        ErrorType.Unauthorized => "unauthorized",
        ErrorType.Forbidden => "forbidden",
        _ => "failure",
    };
}
