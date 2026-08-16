using Lubnan.Domain.Common;
using Lubnan.Domain.Places;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Lubnan.Infrastructure.Persistence.Converters;

/// <summary>
/// How single-valued value objects become columns.
/// </summary>
/// <remarks>
/// A converter rather than an owned type, because <c>Slug</c> and
/// <c>Locale</c> are one value each: an owned type would produce a
/// <c>Slug_Value</c> column and a nullable navigation for no gain.
/// <para>
/// Each converter is paired with a comparer. Without one, EF compares these by
/// reference, decides every loaded entity has changed, and issues an UPDATE for
/// every row it has ever seen.
/// </para>
/// </remarks>
internal static class DomainConverters
{
    public static readonly ValueConverter<Slug, string> Slug = new(
        slug => slug.Value,

        // Create returns a Result because callers can pass anything. A value
        // already in this column was validated on the way in, so a failure here
        // means the row was written around the domain — which is a bug, and
        // throwing is the right answer to a bug.
        value => Domain.Places.Slug.Create(value).Value);

    public static readonly ValueComparer<Slug> SlugComparer = new(
        (left, right) => left!.Equals(right),
        slug => slug.GetHashCode(),
        slug => slug);

    public static readonly ValueConverter<Locale, string> Locale = new(
        locale => locale.Code,
        code => Domain.Common.Locale.ParseOrDefault(code));

    public static readonly ValueComparer<Locale> LocaleComparer = new(
        (left, right) => left!.Equals(right),
        locale => locale.GetHashCode(),
        locale => locale);
}
