namespace Lubnan.Application.Abstractions.Http;

/// <summary>
/// Rate limiting policy names, as constants.
/// </summary>
/// <remarks>
/// A typo in <c>RequireRateLimiting("wrtie")</c> is not an error at build time
/// and not an error at startup — the endpoint simply has no limit, silently,
/// which is the failure you least want to be silent.
/// </remarks>
public static class RateLimits
{
    /// <summary>Generous, per IP. The public catalogue.</summary>
    public const string Read = "read";

    /// <summary>Per user where there is one. Posting, saving, editing.</summary>
    public const string Write = "write";

    /// <summary>
    /// Tight, per IP <em>and</em> per address. Sign-in, registration, password
    /// reset, and anything else that either guesses a credential or sends mail.
    /// </summary>
    /// <remarks>
    /// Separate from <see cref="Write"/> because the threat is different. A
    /// write limit exists to stop one account flooding a feed; this one exists
    /// to stop an unauthenticated attacker working through a password list, and
    /// to stop this API being used as a machine for mailing strangers.
    /// </remarks>
    public const string Auth = "auth";
}
