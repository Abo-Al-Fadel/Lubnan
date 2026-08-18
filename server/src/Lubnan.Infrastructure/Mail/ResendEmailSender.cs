using System.Net.Http.Json;
using Lubnan.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace Lubnan.Infrastructure.Mail;

public sealed class MailOptions
{
    public const string SectionName = "Mail";

    /// <summary>
    /// <c>file</c> writes to disk, <c>resend</c> posts to the Resend API.
    /// </summary>
    /// <remarks>
    /// Development defaults to <c>file</c> so a fresh clone can run the whole
    /// registration flow with no account anywhere and no risk of a test message
    /// reaching a real stranger.
    /// </remarks>
    public string Provider { get; set; } = "file";

    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Must be on a domain verified with the provider. An unverified sender is
    /// accepted by the API and then silently dropped by the receiving side, so
    /// the failure looks like "mail never arrives" rather than like an error.
    /// </summary>
    public string From { get; set; } = "Lubnan <onboarding@resend.dev>";
}

/// <summary>
/// Sends through Resend.
/// </summary>
/// <remarks>
/// Chosen for one reason that matters here: its free tier does not expire and
/// does not need a credit card. Three thousand messages a month is roughly
/// three thousand more than this will send.
/// <para>
/// The whole provider surface is one POST, so there is no SDK — a dependency
/// would be more code to audit than the thing it wraps.
/// </para>
/// </remarks>
internal sealed class ResendEmailSender(HttpClient http, IOptions<MailOptions> options) : IEmailSender
{
    private readonly MailOptions _options = options.Value;

    public async Task SendAsync(OutgoingEmail email, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(email);

        using var response = await http.PostAsJsonAsync(
            "emails",
            new
            {
                from = _options.From,
                to = new[] { email.To },
                subject = email.Subject,
                text = email.Body,
            },
            cancellationToken).ConfigureAwait(false);

        // Throws on failure, deliberately. Every caller that cannot tolerate a
        // send failure already catches it — password reset swallows it so the
        // error path cannot become an account-enumeration oracle — and the
        // outbox retries. A sender that returned quietly on a 4xx would turn a
        // bad API key into "mail silently stopped working", which is the
        // failure nobody notices for a month.
        response.EnsureSuccessStatusCode();
    }
}
