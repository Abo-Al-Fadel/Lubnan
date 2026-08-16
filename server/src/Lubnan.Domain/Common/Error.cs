namespace Lubnan.Domain.Common;

/// <summary>
/// How a failure is classified. The API maps this to a status code in exactly
/// one place, so a handler never names an HTTP code and a new endpoint cannot
/// invent its own convention for "not found".
/// </summary>
public enum ErrorType
{
    /// <summary>Something went wrong that the caller cannot fix. 500.</summary>
    Failure = 0,

    /// <summary>The request was malformed or failed a rule. 400.</summary>
    Validation = 1,

    /// <summary>The thing asked for does not exist. 404.</summary>
    NotFound = 2,

    /// <summary>State has moved since the caller last looked. 409.</summary>
    Conflict = 3,

    /// <summary>We do not know who is asking. 401.</summary>
    Unauthorized = 4,

    /// <summary>We know, and they may not. 403.</summary>
    Forbidden = 5,
}

/// <summary>
/// An expected failure, carrying a stable machine code and a human sentence.
/// </summary>
/// <param name="Code">
/// Dotted and stable, e.g. <c>place.notFound</c>. The frontend switches on
/// this; it must never switch on <paramref name="Message"/>, which is prose
/// and will be translated.
/// </param>
/// <param name="Message">One sentence a person could read. Not a contract.</param>
/// <param name="Type">Decides the status code, in one place, at the edge.</param>
public record Error(string Code, string Message, ErrorType Type = ErrorType.Failure)
{
    public static readonly Error None = new(string.Empty, string.Empty);

    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);

    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);

    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);

    public static Error Unauthorized(string code, string message) => new(code, message, ErrorType.Unauthorized);

    public static Error Forbidden(string code, string message) => new(code, message, ErrorType.Forbidden);

    public static Error Failure(string code, string message) => new(code, message, ErrorType.Failure);
}
