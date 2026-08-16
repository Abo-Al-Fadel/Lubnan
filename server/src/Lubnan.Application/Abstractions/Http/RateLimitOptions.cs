namespace Lubnan.Application.Abstractions.Http;

/// <summary>
/// How generous each rate limiting policy is.
/// </summary>
/// <remarks>
/// Configuration rather than constants, for two reasons that both turned up in
/// practice. An incident sometimes needs a limit tightened now, and a rebuild
/// and redeploy is the wrong length of "now". And a test suite drives hundreds
/// of requests from one address, so a compiled-in limit means tests fail on
/// each other rather than on the code — which is how a suite ends up with the
/// limiter disabled entirely and nobody noticing it broke.
/// <para>
/// The defaults are the production values. A deployment that configures nothing
/// gets the safe numbers.
/// </para>
/// </remarks>
public sealed class RateLimitOptions
{
    public const string SectionName = "RateLimits";

    /// <summary>Reads, per IP, per minute. The public catalogue.</summary>
    public int ReadPermitLimit { get; set; } = 300;

    /// <summary>Writes, per user. Burst size.</summary>
    public int WriteTokenLimit { get; set; } = 20;

    /// <summary>Writes replenished per minute.</summary>
    public int WriteTokensPerPeriod { get; set; } = 5;

    /// <summary>
    /// Sign-in, registration and password reset, per IP.
    /// </summary>
    /// <remarks>
    /// Ten in five minutes is generous for somebody who has forgotten which
    /// password they used, and useless for a dictionary. It is per address
    /// rather than per account on purpose: limiting per account lets anyone who
    /// knows an address lock its owner out of their own sign-in.
    /// </remarks>
    public int AuthPermitLimit { get; set; } = 10;

    public TimeSpan AuthWindow { get; set; } = TimeSpan.FromMinutes(5);
}
