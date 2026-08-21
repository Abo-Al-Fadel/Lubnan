using Lubnan.Api.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Lubnan.Api.IntegrationTests;

/// <summary>
/// The settings check that decides whether a deployment is allowed to boot.
/// </summary>
/// <remarks>
/// The case that matters, and the one that got through: a setting that is
/// <em>present and empty</em>. Re-syncing a Render blueprint left the secrets
/// blank rather than absent, so a null check passed them along and the failure
/// surfaced four frames later as <c>ArgumentException: The value cannot be an
/// empty string (Parameter 'value')</c> — a message that names neither the
/// setting nor the platform nor the fix.
/// </remarks>
public sealed class SettingsPreflightTests
{
    private static readonly string Key = new('k', 48);

    private static IConfiguration Config(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    private static Dictionary<string, string?> Complete() => new()
    {
        ["ConnectionStrings:Database"] = "Host=db;Database=lubnan;Username=u;Password=p",
        ["Auth:SigningKey"] = Key,
        ["Auth:HashKey"] = Key,
        ["Auth:WebBaseUrl"] = "https://lubnan.example",
        ["Mail:Provider"] = "resend",
        ["Mail:ApiKey"] = "re_test",
        ["Mail:From"] = "Lubnan <noreply@lubnan.example>",
    };

    private sealed class Environment(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "Lubnan.Api";
        public string ContentRootPath { get; set; } = ".";
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }

    [Fact]
    public void A_complete_configuration_starts()
    {
        SettingsPreflight.ThrowIfIncomplete(Config(Complete()), new Environment("Production"));
    }

    [Theory]
    [InlineData("ConnectionStrings:Database")]
    [InlineData("Auth:SigningKey")]
    [InlineData("Auth:HashKey")]
    [InlineData("Auth:WebBaseUrl")]
    [InlineData("Mail:ApiKey")]
    [InlineData("Mail:From")]
    public void An_empty_setting_is_refused_the_same_as_a_missing_one(string key)
    {
        var blank = Complete();
        blank[key] = string.Empty;

        var fromBlank = Assert.Throws<InvalidOperationException>(
            () => SettingsPreflight.ThrowIfIncomplete(Config(blank), new Environment("Production")));

        var absent = Complete();
        absent.Remove(key);

        var fromAbsent = Assert.Throws<InvalidOperationException>(
            () => SettingsPreflight.ThrowIfIncomplete(Config(absent), new Environment("Production")));

        // Both name the setting, and neither leaves the reader guessing which.
        Assert.Contains(key, fromBlank.Message, StringComparison.Ordinal);
        Assert.Contains(key, fromAbsent.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Whitespace_is_not_a_value()
    {
        var config = Complete();
        config["Auth:SigningKey"] = "   ";

        var error = Assert.Throws<InvalidOperationException>(
            () => SettingsPreflight.ThrowIfIncomplete(Config(config), new Environment("Production")));

        Assert.Contains("Auth:SigningKey", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_signing_key_short_enough_to_brute_force_is_refused()
    {
        var config = Complete();
        config["Auth:SigningKey"] = "tooshort";

        var error = Assert.Throws<InvalidOperationException>(
            () => SettingsPreflight.ThrowIfIncomplete(Config(config), new Environment("Production")));

        Assert.Contains("Auth:SigningKey", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Everything_wrong_is_reported_at_once()
    {
        // One at a time means crash, read the log, fix one, wait out a
        // redeploy, crash again - four times over, at fifty seconds of cold
        // start each. The whole list costs nothing extra to produce.
        var error = Assert.Throws<InvalidOperationException>(
            () => SettingsPreflight.ThrowIfIncomplete(
                Config(new Dictionary<string, string?> { ["Mail:Provider"] = "resend" }),
                new Environment("Production")));

        foreach (var key in new[]
                 {
                     "ConnectionStrings:Database",
                     "Auth:SigningKey",
                     "Auth:HashKey",
                     "Auth:WebBaseUrl",
                     "Mail:ApiKey",
                     "Mail:From",
                 })
        {
            Assert.Contains(key, error.Message, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void The_file_mail_provider_needs_no_api_key()
    {
        var config = Complete();
        config["Mail:Provider"] = "file";
        config["Mail:ApiKey"] = string.Empty;
        config["Mail:From"] = string.Empty;

        SettingsPreflight.ThrowIfIncomplete(Config(config), new Environment("Production"));
    }

    [Fact]
    public void Development_is_left_alone()
    {
        // A fresh clone must run. appsettings.Development.json supplies working
        // placeholders, and demanding production secrets locally would make the
        // first `dotnet run` fail for no reason.
        SettingsPreflight.ThrowIfIncomplete(
            Config([]),
            new Environment("Development"));
    }
}
