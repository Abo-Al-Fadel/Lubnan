namespace Lubnan.Domain.Common;

/// <summary>
/// Something with an identity that outlives its values. Two entities are the
/// same entity when their ids match, however much else has changed.
/// </summary>
public abstract class Entity : IEquatable<Entity>
{
    protected Entity(Guid id) => Id = id;

    /// <summary>EF materialises through this; nothing else should.</summary>
    protected Entity() { }

    public Guid Id { get; protected init; }

    public bool Equals(Entity? other) =>
        other is not null && other.GetType() == GetType() && other.Id == Id;

    public override bool Equals(object? obj) => obj is Entity e && Equals(e);

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    public static bool operator ==(Entity? a, Entity? b) => Equals(a, b);

    public static bool operator !=(Entity? a, Entity? b) => !Equals(a, b);
}
