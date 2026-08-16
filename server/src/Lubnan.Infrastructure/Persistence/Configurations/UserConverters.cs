using Lubnan.Domain.Users;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Lubnan.Infrastructure.Persistence.Configurations;

/// <summary>How the user value objects become columns.</summary>
/// <remarks>
/// Reading back calls <c>.Value</c> on a Result that was validated on the way
/// in. If it throws, a row was written around the domain — by a migration, by
/// psql, by an import — and that is a bug rather than a runtime condition, so
/// failing loudly is correct.
/// </remarks>
internal static class UserConverters
{
    public static readonly ValueConverter<Email, string> Email = new(
        email => email.Value,
        value => Domain.Users.Email.Create(value).Value);

    public static readonly ValueComparer<Email> EmailComparer = new(
        (left, right) => left!.Equals(right),
        email => email.GetHashCode(),
        email => email);

    public static readonly ValueConverter<DisplayName, string> DisplayName = new(
        name => name.Value,
        value => Domain.Users.DisplayName.Create(value).Value);

    public static readonly ValueComparer<DisplayName> DisplayNameComparer = new(
        (left, right) => left!.Equals(right),
        name => name.GetHashCode(),
        name => name);
}
