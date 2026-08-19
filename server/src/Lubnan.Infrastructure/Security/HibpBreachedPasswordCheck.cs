using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Lubnan.Application.Abstractions.Security;
using Microsoft.Extensions.Logging;

namespace Lubnan.Infrastructure.Security;

/// <summary>
/// Checks a password against Have I Been Pwned, without sending the password.
/// </summary>
/// <remarks>
/// <b>k-anonymity, and it is the whole reason this is acceptable at all.</b>
/// Sending a password — or even its full hash — to a third party in order to
/// ask whether it is safe would be a spectacular own goal. Instead the password
/// is hashed with SHA-1 locally, and only the <em>first five hex characters</em>
/// of that hash leave this process. The service answers with every suffix it
/// holds under that prefix — several hundred of them — and the comparison
/// happens here.
/// <para>
/// So the remote end learns that somebody, somewhere, has a password whose hash
/// begins with those five characters. That set contains hundreds of thousands
/// of real passwords. It cannot identify ours, and it never sees the account
/// the question was asked for.
/// </para>
/// <para>
/// SHA-1 is correct here despite being broken for signatures. It is not
/// protecting anything: it is the index the corpus is published under, and a
/// collision would only cause a false positive that asks somebody to pick a
/// different password.
/// </para>
/// <para>
/// No API key, no account, no cost. The range endpoint is free and unmetered,
/// which is why this could be added without another dashboard to configure.
/// </para>
/// </remarks>
internal sealed class HibpBreachedPasswordCheck(
    HttpClient http,
    ILogger<HibpBreachedPasswordCheck> logger) : IBreachedPasswordCheck
{
    public async Task<bool> IsBreachedAsync(string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(password))
        {
            return false;
        }

#pragma warning disable CA5350 // SHA-1 is the corpus index, not a security control. See the remarks.
        var hash = Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(password)));
#pragma warning restore CA5350

        var prefix = hash[..5];
        var suffix = hash[5..];

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"range/{prefix}");

            // Padding makes every response a similar size, so an observer who
            // can see the encrypted traffic cannot infer the prefix from the
            // length of the reply.
            request.Headers.Add("Add-Padding", "true");

            using var response = await http.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            foreach (var line in body.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var separator = line.IndexOf(':', StringComparison.Ordinal);
                if (separator <= 0)
                {
                    continue;
                }

                if (!line.AsSpan(0, separator).Trim().Equals(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Padded entries are real suffixes with a count of zero. Treating
                // one as a hit would reject a perfectly good password for the
                // sake of a privacy feature.
                var count = line.AsSpan(separator + 1).Trim();
                return !count.SequenceEqual("0") && long.TryParse(count, NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out var seen) && seen > 0;
            }

            return false;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            // Fail open, deliberately and loudly.
            //
            // A third-party outage is not a reason to stop people opening
            // accounts. Throwing here would make api.pwnedpasswords.com a hard
            // dependency of registration, which trades a rare, mild risk for a
            // total one.
            logger.BreachCheckUnavailable(ex);
            return false;
        }
    }
}

internal static partial class HibpLog
{
    [LoggerMessage(
        EventId = 4300,
        Level = LogLevel.Warning,
        Message = "Breached-password check was unavailable; the password was allowed unchecked.")]
    public static partial void BreachCheckUnavailable(this ILogger logger, Exception exception);
}
