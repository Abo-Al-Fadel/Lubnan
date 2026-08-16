namespace Lubnan.Domain.Common;

/// <summary>
/// Something that has already happened, named in the past tense. Events are
/// how one slice tells another that the world moved without either of them
/// holding a reference to the other.
/// </summary>
public interface IDomainEvent
{
    /// <summary>Stable across a retry, so a consumer can deduplicate.</summary>
    Guid EventId { get; }

    DateTimeOffset OccurredAt { get; }
}

/// <summary>
/// Base record for events. Inherit and add the payload:
/// <c>public sealed record PlacePublished(Guid PlaceId) : DomainEvent;</c>
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();

    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
