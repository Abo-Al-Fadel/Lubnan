namespace Lubnan.Domain.Common;

/// <summary>
/// Something defined entirely by its values, with no identity of its own. Two
/// coordinates at the same point are the same coordinate.
/// </summary>
/// <remarks>
/// Value objects here validate on construction and expose no setter, so an
/// invalid one cannot exist anywhere in the system. That moves a whole class
/// of check out of handlers: a method taking a <c>Slug</c> does not have to
/// ask whether the slug is well formed.
/// </remarks>
public abstract class ValueObject : IEquatable<ValueObject>
{
    /// <summary>The values that constitute this object, in a stable order.</summary>
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public bool Equals(ValueObject? other) =>
        other is not null
        && other.GetType() == GetType()
        && other.GetEqualityComponents().SequenceEqual(GetEqualityComponents());

    public override bool Equals(object? obj) => obj is ValueObject v && Equals(v);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var component in GetEqualityComponents())
        {
            hash.Add(component);
        }

        return hash.ToHashCode();
    }

    public static bool operator ==(ValueObject? a, ValueObject? b) => Equals(a, b);

    public static bool operator !=(ValueObject? a, ValueObject? b) => !Equals(a, b);
}
