using Lubnan.Infrastructure.Persistence;
using Xunit;

namespace Lubnan.Api.IntegrationTests;

/// <summary>
/// The connection string a dashboard hands you has to work as pasted.
/// </summary>
/// <remarks>
/// No <c>[Collection]</c>, so it never starts the Postgres container: this is a
/// string function and wants neither a host nor a database. It lives here only
/// because this is the test project that already references Infrastructure —
/// Lubnan.Domain.Tests deliberately references nothing but the domain.
/// </remarks>
public sealed class ConnectionStringTests
{
    [Fact]
    public void A_neon_pooled_uri_becomes_something_npgsql_understands()
    {
        // Exactly the shape Neon prints, pooler host and all.
        const string neon =
            "postgresql://lubnan_owner:npg_S3cr3t@ep-cool-fire-a1b2c3-pooler.eu-central-1.aws.neon.tech/neondb?sslmode=require&channel_binding=require";

        var result = ConnectionString.Normalise(neon);

        Assert.Contains("Host=ep-cool-fire-a1b2c3-pooler.eu-central-1.aws.neon.tech", result, StringComparison.Ordinal);
        Assert.Contains("Database=neondb", result, StringComparison.Ordinal);
        Assert.Contains("Username=lubnan_owner", result, StringComparison.Ordinal);
        Assert.Contains("Password=npg_S3cr3t", result, StringComparison.Ordinal);
        Assert.Contains("SSL Mode=Require", result, StringComparison.Ordinal);

        // channel_binding is a libpq option Npgsql has never heard of. Passing
        // it through would throw on an unknown keyword, which is a failure to
        // start rather than a connection problem.
        Assert.DoesNotContain("channel_binding", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void A_keyword_string_is_left_exactly_as_it_was()
    {
        const string keywords = "Host=localhost;Port=5433;Database=lubnan;Username=lubnan;Password=lubnan";

        // Local development already works. A normaliser that "helpfully"
        // rewrote a string that was already correct would be a new way to break
        // the one setup that was never broken.
        Assert.Equal(keywords, ConnectionString.Normalise(keywords));
    }

    [Fact]
    public void A_password_with_url_punctuation_survives()
    {
        // Generated passwords contain these, and a URI percent-encodes them.
        // Forgetting to decode produces an authentication failure that says the
        // password is wrong rather than that it was mangled in transit.
        const string uri = "postgres://user:p%40ss%3Aword%2F1@db.example.com:5432/app";

        var result = ConnectionString.Normalise(uri);

        Assert.Contains("Password=p@ss:word/1", result, StringComparison.Ordinal);
        Assert.Contains("Port=5432", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Redacting_hides_the_password_and_keeps_the_rest()
    {
        const string uri = "postgresql://user:hunter2@db.example.com/app?sslmode=require";

        var redacted = ConnectionString.Redact(uri);

        // A startup failure is the one moment somebody wants to see this
        // string, and the one moment it is most likely to be pasted into an
        // issue tracker.
        Assert.DoesNotContain("hunter2", redacted, StringComparison.Ordinal);
        Assert.Contains("db.example.com", redacted, StringComparison.Ordinal);
    }
}
