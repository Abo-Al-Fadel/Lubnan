using Lubnan.Domain.Common;

namespace Lubnan.Domain.Users;

/// <summary>An email address, normalised so one person cannot hold two accounts.</summary>
/// <remarks>
/// Lowercased on the way in. The RFC says the local part is case-sensitive;
/// no mail provider anyone uses actually treats it that way, and honouring the
/// spec here would let <c>Ismael@…</c> and <c>ismael@…</c> register separately,
/// then let one of them reset the other's password. Practice beats the letter.
/// <para>
/// Validation is deliberately shallow — one <c>@</c>, something either side, a
/// dot in the domain. Anything stricter rejects addresses that genuinely work,
/// and the only real proof an address exists is that a message sent to it
/// arrived. That is what <see cref="User.ConfirmEmail"/> is for.
/// </para>
/// </remarks>
public sealed class Email : ValueObject
{
    /// <summary>The maximum an SMTP path may be, per RFC 5321.</summary>
    public const int MaxLength = 254;

    private Email(string value) => Value = value;

    public string Value { get; }

    /// <summary>The part after the @, for abuse heuristics and nothing else.</summary>
    public string Domain => Value[(Value.IndexOf('@', StringComparison.Ordinal) + 1)..];

    public static Result<Email> Create(string? value)
    {
        var candidate = value?.Trim().ToLowerInvariant();

        if (string.IsNullOrEmpty(candidate) || candidate.Length > MaxLength)
        {
            return Result.Failure<Email>(Error.Validation(
                "email.invalid", "Enter an email address."));
        }

        var at = candidate.IndexOf('@', StringComparison.Ordinal);

        var wellFormed =
            at > 0
            && at == candidate.LastIndexOf('@')
            && at < candidate.Length - 1
            && candidate[(at + 1)..].Contains('.', StringComparison.Ordinal)
            && !candidate.EndsWith('.')
            && !candidate.Contains(' ', StringComparison.Ordinal);

        return wellFormed
            ? Result.Success(new Email(candidate))
            : Result.Failure<Email>(Error.Validation("email.invalid", "That does not look like an email address."));
    }

    protected override IEnumerable<object?> GetEqualityComponents() => [Value];

    public override string ToString() => Value;
}
