using Lubnan.Application.Abstractions.Security;
using Lubnan.Application.Features.Identity;
using Microsoft.Extensions.Options;

namespace Lubnan.Infrastructure.Security;

/// <summary>Keyed IP hashing, so the address is never stored.</summary>
internal sealed class IpHasher(IOptions<AuthOptions> options) : IIpHasher
{
    public string? Hash(string? ip) => Hashing.HashIp(ip, options.Value.HashKey);
}

/// <summary>The one-way marker a deleted address leaves behind.</summary>
internal sealed class EmailTombstoner(IOptions<AuthOptions> options) : IEmailTombstoner
{
    public string Tombstone(string email) => Hashing.EmailTombstone(email, options.Value.HashKey);
}
