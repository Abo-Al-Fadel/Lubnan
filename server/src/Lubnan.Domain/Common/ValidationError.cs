namespace Lubnan.Domain.Common;

/// <summary>
/// A validation failure that names the fields, so the response can say which
/// input was wrong instead of only that something was.
/// </summary>
/// <remarks>
/// Keyed by field name, many messages per field, which is the shape RFC 7807's
/// <c>errors</c> member takes and the shape a form needs to put a message
/// under the right input.
/// </remarks>
public sealed record ValidationError(IReadOnlyDictionary<string, string[]> Failures)
    : Error("request.invalid", "One or more fields are invalid.", ErrorType.Validation)
{
    public static ValidationError From(IEnumerable<(string Field, string Message)> failures) =>
        new(failures
            .GroupBy(f => f.Field, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Select(f => f.Message).ToArray(), StringComparer.Ordinal));
}
