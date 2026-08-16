using Lubnan.Domain.Common;

namespace Lubnan.Domain.Places;

/// <summary>
/// The URL-safe identifier a place is reached by: <c>/explore/byblos</c>.
/// </summary>
/// <remarks>
/// A slug is part of the public contract — it appears in links people share
/// and in search results — so it is deliberately narrow: lowercase ASCII
/// letters, digits and single hyphens. Anything that would need percent
/// encoding is rejected at construction rather than escaped later, because
/// escaping later means two spellings of the same URL.
/// </remarks>
public sealed class Slug : ValueObject
{
    public const int MaxLength = 80;

    private Slug(string value) => Value = value;

    public string Value { get; }

    public static Result<Slug> Create(string? value)
    {
        var candidate = value?.Trim().ToLowerInvariant();

        if (string.IsNullOrEmpty(candidate))
        {
            return Result.Failure<Slug>(Error.Validation("slug.empty", "A slug is required."));
        }

        if (candidate.Length > MaxLength)
        {
            return Result.Failure<Slug>(
                Error.Validation("slug.tooLong", $"A slug may be at most {MaxLength} characters."));
        }

        if (!IsWellFormed(candidate))
        {
            return Result.Failure<Slug>(Error.Validation(
                "slug.malformed",
                "A slug may contain lowercase letters, digits and single hyphens between them."));
        }

        return Result.Success(new Slug(candidate));
    }

    private static bool IsWellFormed(string value)
    {
        if (value[0] == '-' || value[^1] == '-')
        {
            return false;
        }

        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            var allowed = c is >= 'a' and <= 'z' or >= '0' and <= '9' || c == '-';

            if (!allowed || (c == '-' && value[i - 1] == '-'))
            {
                return false;
            }
        }

        return true;
    }

    protected override IEnumerable<object?> GetEqualityComponents() => [Value];

    public override string ToString() => Value;
}
