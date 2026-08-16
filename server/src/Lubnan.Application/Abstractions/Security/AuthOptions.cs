using System.ComponentModel.DataAnnotations;

namespace Lubnan.Application.Abstractions.Security;

/// <summary>
/// Everything about sessions that a deployment decides.
/// </summary>
/// <remarks>
/// Validated at startup, not at first use. A missing or short signing key
/// discovered when somebody tries to sign in means the application boots,
/// passes its health check, rolls out to every replica, and then fails on the
/// first real request. Failing to start is louder and far cheaper.
/// </remarks>
public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    /// <summary>
    /// The HMAC key for access tokens. Never in appsettings.json.
    /// </summary>
    /// <remarks>
    /// User secrets locally, an environment variable or secret store in a
    /// deployment. Thirty-two characters minimum because HMAC-SHA256 is no
    /// stronger than its key, and a short one turns "forge an access token"
    /// into an offline brute force against a value that never rotates.
    /// </remarks>
    [Required(ErrorMessage = "Auth:SigningKey is required. Set it with: dotnet user-secrets set \"Auth:SigningKey\" \"<48+ random chars>\"")]
    [MinLength(32, ErrorMessage = "Auth:SigningKey must be at least 32 characters.")]
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>Keyed hashing for IP addresses and email tombstones.</summary>
    /// <remarks>
    /// Separate from the signing key on purpose. One key, one job: reusing the
    /// token key here would mean that anything which leaks a hash also weakens
    /// the ability to forge tokens.
    /// </remarks>
    [Required(ErrorMessage = "Auth:HashKey is required.")]
    [MinLength(32, ErrorMessage = "Auth:HashKey must be at least 32 characters.")]
    public string HashKey { get; set; } = string.Empty;

    [Required]
    public string Issuer { get; set; } = "lubnan-api";

    [Required]
    public string Audience { get; set; } = "lubnan-web";

    /// <summary>
    /// Short, because it is validated without a database read and so cannot be
    /// revoked before it expires. This is the length of the window in which a
    /// signed-out session still works.
    /// </summary>
    public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Long, because it is checked against a row on every use and can be
    /// revoked instantly. Rotation keeps the stolen-token window at one
    /// refresh.
    /// </summary>
    public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromDays(30);

    /// <summary>
    /// Off only for local HTTP. Never off where a real person signs in: a
    /// cookie without Secure travels over plain HTTP, which is the whole
    /// attack.
    /// </summary>
    public bool RequireSecureCookies { get; set; } = true;

    /// <summary>Where confirmation and reset links point. The frontend origin.</summary>
    [Required]
    public string WebBaseUrl { get; set; } = "http://localhost:3000";
}
