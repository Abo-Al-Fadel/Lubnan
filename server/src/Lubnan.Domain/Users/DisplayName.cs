using System.Buffers;
using System.Globalization;
using Lubnan.Domain.Common;

namespace Lubnan.Domain.Users;

/// <summary>What other people see next to a post.</summary>
/// <remarks>
/// Attacker-controlled text that renders beside other users' content, so the
/// rules are about what it can be made to look like rather than about taste:
/// no control characters, no bidirectional overrides, no runs of whitespace.
/// <para>
/// The bidi rule matters on a trilingual site. U+202E flips the rendering
/// direction of everything after it, and a name containing one can be made to
/// display as something else entirely — including as a different user's name.
/// Escaping at render time does not help, because these are legitimate
/// characters in Arabic text; the fix is to refuse the *overrides* while
/// leaving ordinary Arabic alone.
/// </para>
/// </remarks>
public sealed class DisplayName : ValueObject
{
    public const int MinLength = 2;
    public const int MaxLength = 40;

    // Explicit directional formatting. Arabic does not need these to render
    // correctly; the Unicode bidi algorithm handles real text on its own.
    private static readonly SearchValues<char> BidiOverrides = SearchValues.Create(
    [
        '‪', '‫', '‬', '‭', '‮',
        '⁦', '⁧', '⁨', '⁩',
    ]);

    private DisplayName(string value) => Value = value;

    public string Value { get; }

    public static Result<DisplayName> Create(string? value)
    {
        // Collapse internal whitespace before measuring, so forty spaces is not
        // a forty-character name.
        var candidate = string.Join(' ', (value ?? string.Empty)
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        if (candidate.Length is < MinLength or > MaxLength)
        {
            return Result.Failure<DisplayName>(Error.Validation(
                "displayName.length", $"A display name is between {MinLength} and {MaxLength} characters."));
        }

        if (candidate.Any(char.IsControl) || candidate.AsSpan().IndexOfAny(BidiOverrides) >= 0)
        {
            return Result.Failure<DisplayName>(Error.Validation(
                "displayName.characters", "A display name cannot contain formatting or control characters."));
        }

        // A name made only of punctuation or symbols is not a name, and it is
        // what somebody uses to impersonate a UI element.
        if (!candidate.Any(c => char.GetUnicodeCategory(c)
            is UnicodeCategory.LowercaseLetter or UnicodeCategory.UppercaseLetter
            or UnicodeCategory.OtherLetter or UnicodeCategory.TitlecaseLetter
            or UnicodeCategory.DecimalDigitNumber))
        {
            return Result.Failure<DisplayName>(Error.Validation(
                "displayName.characters", "A display name needs at least one letter or digit."));
        }

        return Result.Success(new DisplayName(candidate));
    }

    protected override IEnumerable<object?> GetEqualityComponents() => [Value];

    public override string ToString() => Value;
}
