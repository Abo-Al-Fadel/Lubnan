using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Lubnan.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps a locale-keyed dictionary of small records to a single <c>jsonb</c>
/// column.
/// </summary>
/// <remarks>
/// The rule this codebase follows, applied consistently: <b>a column for
/// anything you filter, sort, constrain or search on; JSON for prose that is
/// only ever read as part of its parent.</b>
/// <para>
/// So a place's article gets a table — it is searched, it needs a per-locale
/// index, and "which places lack Arabic" has to be answerable. A callout's
/// label does not: nothing queries it, and it is read exactly when its callout
/// is read. Putting it in a table would add a join and a migration for every
/// new locale to buy nothing.
/// </para>
/// </remarks>
internal static class JsonDictionary
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        // Stored bytes, not a wire format. Nobody reads this indented, and
        // whitespace in a jsonb column is discarded by Postgres anyway.
        WriteIndented = false,
    };

    public static ValueConverter<Dictionary<string, TValue>, string> Converter<TValue>() => new(
        value => JsonSerializer.Serialize(value, Options),
        json => JsonSerializer.Deserialize<Dictionary<string, TValue>>(json, Options)
                ?? new Dictionary<string, TValue>(StringComparer.Ordinal));

    /// <summary>
    /// Without this, EF compares dictionaries by reference, concludes every
    /// loaded row has changed, and writes them all back on the next save.
    /// The snapshot has to be a real copy or change tracking compares an object
    /// with itself and never sees an edit.
    /// </summary>
    public static ValueComparer<Dictionary<string, TValue>> Comparer<TValue>() => new(
        (left, right) => JsonSerializer.Serialize(left, Options) == JsonSerializer.Serialize(right, Options),
        value => JsonSerializer.Serialize(value, Options).GetHashCode(StringComparison.Ordinal),
        value => new Dictionary<string, TValue>(value, StringComparer.Ordinal));
}
