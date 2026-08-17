using System.Buffers;

namespace Lubnan.Domain.Common;

/// <summary>
/// Marks that flip or hide text. Arabic does not need them; attackers do.
/// </summary>
public static class TextRules
{
    // Explicit directional formatting. The Unicode bidi algorithm handles
    // real Arabic on its own; these overrides exist to make one string
    // render as another.
    private static readonly SearchValues<char> BidiOverrides = SearchValues.Create(
    [
        '\u202A', '\u202B', '\u202C', '\u202D', '\u202E',
        '\u2066', '\u2067', '\u2068', '\u2069',
    ]);

    public static bool HasForbiddenMarks(string value) =>
        value.Any(char.IsControl) || value.AsSpan().IndexOfAny(BidiOverrides) >= 0;
}
