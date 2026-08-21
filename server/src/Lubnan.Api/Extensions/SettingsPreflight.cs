using Lubnan.Application.Abstractions.Security;
using Lubnan.Infrastructure.Mail;

namespace Lubnan.Api.Extensions;

/// <summary>
/// Refuse to start on a deployment that is missing its settings, and say all of
/// them at once.
/// </summary>
/// <remarks>
/// This exists because of the way the settings actually went missing. Adding
/// one key to <c>render.yaml</c> re-synced the blueprint, and the values marked
/// <c>sync: false</c> — which live only in the dashboard, because they are
/// secrets — came back empty. Not absent: <em>empty</em>.
/// <para>
/// That distinction was the whole problem. The connection-string check was
/// <c>?? throw new InvalidOperationException("ConnectionStrings:Database is not
/// configured…")</c>, which catches null and not <c>""</c>, so the empty value
/// sailed past a message written for exactly this situation and landed on
/// <c>ArgumentException: The value cannot be an empty string (Parameter
/// 'value')</c>. A stack trace naming a parameter, for a missing environment
/// variable. Every guard here treats blank and absent as the same thing.
/// </para>
/// <para>
/// Reported together, too. One at a time means crash, read log, fix one, wait
/// out a redeploy, crash again — four times over, on a platform where a cold
/// start is fifty seconds. Nothing is learned in that loop that could not have
/// been said on the first pass.
/// </para>
/// <para>
/// Skipped in Development, where <c>appsettings.Development.json</c> supplies
/// working values and a fresh clone must simply run.
/// </para>
/// </remarks>
internal static class SettingsPreflight
{
    public static void ThrowIfIncomplete(IConfiguration configuration, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        if (environment.IsDevelopment())
        {
            return;
        }

        var missing = new List<string>();

        void Require(string key, string hint, int minimumLength = 1)
        {
            var value = configuration[key];

            if (string.IsNullOrWhiteSpace(value))
            {
                missing.Add($"  {key}\n      {hint}");
            }
            else if (value.Trim().Length < minimumLength)
            {
                missing.Add($"  {key}\n      Set, but shorter than {minimumLength} characters. {hint}");
            }
        }

        Require(
            "ConnectionStrings:Database",
            "The Neon connection string. Either shape works - postgresql://… or Host=…;Database=…");

        Require(
            "Auth:SigningKey",
            "Signs access tokens. Generate with: openssl rand -base64 48",
            minimumLength: 32);

        Require(
            "Auth:HashKey",
            "Hashes refresh tokens and IP addresses. A DIFFERENT one: openssl rand -base64 48",
            minimumLength: 32);

        // Not cosmetic. Confirmation and password-reset links are built from
        // this, so an empty value emails people a path with no host - a dead
        // link carrying a live token, and a failure nobody sees until somebody
        // outside the team tries to register.
        Require(
            "Auth:WebBaseUrl",
            "The public frontend origin, e.g. https://lubnan.vercel.app - confirmation links are built from it.");

        var provider = configuration[$"{MailOptions.SectionName}:Provider"];

        if (string.Equals(provider, "resend", StringComparison.OrdinalIgnoreCase))
        {
            Require("Mail:ApiKey", "Resend API key, with Sending access.");
            Require("Mail:From", "A sender on a domain verified with Resend, e.g. Lubnan <noreply@yourdomain>");
        }

        if (missing.Count == 0)
        {
            return;
        }

        throw new InvalidOperationException(
            $"""
             Cannot start: {missing.Count} required setting(s) are missing or blank in {environment.EnvironmentName}.

             {string.Join("\n", missing)}

             Set them as environment variables, with a double underscore for the colon
             (ConnectionStrings__Database, Auth__SigningKey, and so on).

             On Render these live in the service's Environment tab, not in render.yaml -
             the blueprint marks them `sync: false` precisely so they are never committed.
             Re-syncing a blueprint can leave them blank, which is what this message is for.
             """);
    }
}
