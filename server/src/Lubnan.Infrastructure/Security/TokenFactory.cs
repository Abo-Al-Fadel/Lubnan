using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Lubnan.Application.Abstractions;
using Lubnan.Application.Abstractions.Security;
using Lubnan.Domain.Users;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Lubnan.Infrastructure.Security;

/// <summary>Everything token-shaped, in one place.</summary>
internal sealed class TokenFactory(IOptions<AuthOptions> options, IClock clock) : ITokenFactory
{
    /// <summary>256 bits. Below 128 is guessable; above 256 buys nothing.</summary>
    private const int TokenBytes = 32;

    private readonly AuthOptions _options = options.Value;

    private readonly SymmetricSecurityKey _signingKey =
        new(Encoding.UTF8.GetBytes(options.Value.SigningKey));

    public (string Token, DateTimeOffset ExpiresAt) CreateAccessToken(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var now = clock.UtcNow;
        var expiresAt = now + _options.AccessTokenLifetime;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),

            // A unique id per token, so an individual one can be denied later
            // without denying the user.
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),

            // The security stamp. Present from the first release so that
            // checking it against a cached value later is a change to
            // validation rather than to the token format - the latter would
            // sign every user out on deploy.
            new(Claims.SecurityStamp, user.SecurityStamp.ToString()),
        };

        // Only when true. An "isAdmin: false" claim is a larger token on every
        // request that says nothing the absence would not.
        if (user.IsAdmin)
        {
            claims.Add(new Claim(ClaimTypes.Role, Roles.Admin));
        }

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256));

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    public RefreshToken CreateRefreshToken() => Create();

    public RefreshToken CreatePurposeToken() => Create();

    public string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)))
            .ToLower(CultureInfo.InvariantCulture);

    private RefreshToken Create()
    {
        // RandomNumberGenerator, not Random and not Guid.NewGuid. Both of those
        // are fine for identifiers and neither is documented as suitable for
        // secrets - which is exactly how guessable session tokens happen.
        var bytes = RandomNumberGenerator.GetBytes(TokenBytes);

        // Base64Url, because this travels in a URL for confirmation links and
        // ordinary base64 contains + / and = .
        var value = Base64UrlEncoder.Encode(bytes);

        return new RefreshToken(value, HashToken(value));
    }
}

/// <summary>Claim types this application defines.</summary>
public static class Claims
{
    public const string SecurityStamp = "stamp";
}

/// <summary>Roles, as constants, so a typo is a build error and not a silent grant.</summary>
public static class Roles
{
    public const string Admin = "admin";
}
