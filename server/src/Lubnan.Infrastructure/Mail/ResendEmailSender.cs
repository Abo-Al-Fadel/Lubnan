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

        if (response.IsSuccessStatusCode)
        {
            return;
        }

        // Read the body before throwing, because the body is the entire
        // diagnosis and EnsureSuccessStatusCode discards it.
        //
        // Resend answers a rejected send with a 4xx and a JSON explanation:
        // "The from address is not verified", "domain not found", "invalid API
        // key". Throwing on the status alone produced `outbox_messages.error`
        // rows that said "Response status code does not indicate success: 403"
        // — true, useless, and indistinguishable between three unrelated
        // misconfigurations. A rejected send also never appears in Resend's own
        // Emails log, so that body is the only place the reason exists at all.
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        throw new HttpRequestException(
            $"Resend refused the message ({(int)response.StatusCode} {response.StatusCode}): "
            + (body.Length > 400 ? body[..400] : body),
            null,
            response.StatusCode);
    }
}
