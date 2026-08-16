namespace Lubnan.Infrastructure.Persistence.Outbox;

/// <summary>
/// A domain event, written in the same transaction as the change that raised
/// it, for something else to pick up afterwards.
/// </summary>
/// <remarks>
/// The problem this solves: "save the post" and "publish PostSubmitted" are
/// two operations against two systems, and any pair of operations can fail
/// between the first and the second. Publish first and you eventually announce
/// a write that rolled back. Save first and you eventually lose an event.
/// <para>
/// Writing the event to the same database, in the same transaction, makes the
/// pair atomic. A separate processor then delivers it, retrying until it is
/// acknowledged — which converts the problem from "exactly once", which is not
/// available, into "at least once", which is, provided consumers deduplicate on
/// <see cref="Id"/>.
/// </para>
/// <para>
/// This is an infrastructure concern and lives here on purpose: the domain
/// raises events and has no idea they are being persisted.
/// </para>
/// </remarks>
public sealed class OutboxMessage
{
    /// <summary>
    /// The domain event's own id, not a fresh one. It survives a retry, so a
    /// consumer can recognise a redelivery and ignore it.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>Assembly-qualified enough to deserialise, short enough to read.</summary>
    public required string Type { get; init; }

    public required string Payload { get; init; }

    public DateTimeOffset OccurredAt { get; init; }

    /// <summary>Null until delivered. The work queue is exactly the null rows.</summary>
    public DateTimeOffset? ProcessedAt { get; set; }

    public int Attempts { get; set; }

    /// <summary>The last failure, kept so a stuck message can be diagnosed.</summary>
    public string? Error { get; set; }
}
