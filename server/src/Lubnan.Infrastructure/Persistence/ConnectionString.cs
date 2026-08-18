using System.Globalization;
using Npgsql;

namespace Lubnan.Infrastructure.Persistence;

/// <summary>
/// Accepts a connection string in either of the two shapes the world hands out.
/// </summary>
/// <remarks>
/// Npgsql wants ADO.NET keywords — <c>Host=…;Database=…;Username=…</c>. Every
/// managed platform hands you a URI instead: Neon, Supabase, Render, Railway
/// and Heroku all print
/// <c>postgresql://user:password@host/db?sslmode=require</c>, and
/// <c>DATABASE_URL</c> in that form is close to a standard.
/// <para>
/// Npgsql does not parse it. Pasting the string a dashboard gave you produces
/// <c>Format of the initialization string does not conform to specification
/// starting at index 0</c> — which names neither the setting nor the fix, and
/// arrives at the first database call rather than at startup.
/// </para>
/// <para>
/// So both are accepted. This is a five-minute problem that costs an hour when
/// it happens during a first deployment, with three other new services in play
/// and no obvious reason to suspect the connection string's punctuation.
/// </para>
/// </remarks>
public static class ConnectionString
{
    /// <summary>Convert a URI-style connection string; pass keyword-style through.</summary>
    public static string Normalise(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var trimmed = value.Trim();

        if (!trimmed.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
            && !trimmed.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed;
        }

        var uri = new Uri(trimmed);

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Database = uri.AbsolutePath.Trim('/'),

            // Managed Postgres is always over the public internet, so TLS is
            // not optional. Require rather than VerifyFull because Neon and
            // Supabase both front the database with a pooler whose certificate
            // does not match the hostname you connect to.
            SslMode = SslMode.Require,
        };

        if (uri.Port > 0)
        {
            builder.Port = uri.Port;
        }

        var credentials = uri.UserInfo.Split(':', 2);

        if (credentials.Length > 0 && credentials[0].Length > 0)
        {
            // The password routinely contains characters that are percent-
            // encoded in a URI. Decoding is not cosmetic: a password with a
            // literal '@' or '#' fails authentication otherwise, and the error
            // says the password is wrong rather than that it was mangled.
            builder.Username = Uri.UnescapeDataString(credentials[0]);
        }

        if (credentials.Length > 1)
        {
            builder.Password = Uri.UnescapeDataString(credentials[1]);
        }

        foreach (var pair in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            if (parts.Length != 2)
            {
                continue;
            }

            var key = Uri.UnescapeDataString(parts[0]);
            var setting = Uri.UnescapeDataString(parts[1]);

            // sslmode is already decided above. channel_binding and the
            // pooler's own hints are libpq options Npgsql does not know, and
            // passing them through would throw on an unknown keyword.
            if (key.Equals("sslmode", StringComparison.OrdinalIgnoreCase)
                || key.Equals("channel_binding", StringComparison.OrdinalIgnoreCase)
                || key.Equals("options", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            try
            {
                builder[key] = setting;
            }
            catch (ArgumentException)
            {
                // An unrecognised query parameter is the platform's business,
                // not ours. Dropping it beats refusing to start.
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// A version safe to put in a log or an error message.
    /// </summary>
    /// <remarks>
    /// Startup failures are the one moment somebody genuinely wants to see the
    /// connection string, and the one moment it is most likely to be pasted
    /// into an issue.
    /// </remarks>
    public static string Redact(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "(empty)";
        }

        try
        {
            var builder = new NpgsqlConnectionStringBuilder(Normalise(value))
            {
                Password = "***",
            };

            return builder.ToString();
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException or UriFormatException)
        {
            return string.Create(
                CultureInfo.InvariantCulture,
                $"(unparseable, {value.Length} characters)");
        }
    }
}
