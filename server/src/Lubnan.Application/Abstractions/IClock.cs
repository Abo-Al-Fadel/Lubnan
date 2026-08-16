namespace Lubnan.Application.Abstractions;

/// <summary>The current time, injected rather than read from a static.</summary>
/// <remarks>
/// A handler that calls <c>DateTimeOffset.UtcNow</c> cannot be tested at a
/// specific instant, so every rule that depends on time — token expiry, a
/// trending window, a rate limit, "published yesterday" — becomes testable
/// only by waiting. Two lines of abstraction buy determinism for all of them.
/// </remarks>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
