using System.Text.Json;
using Lubnan.Domain.Common;
using Lubnan.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Lubnan.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Drains domain events off the aggregates being saved and writes them to the
/// outbox in the same transaction.
/// </summary>
/// <remarks>
/// This is the whole transactional-outbox trick in forty lines. The events are
/// turned into rows <em>before</em> the save completes, so they are part of the
/// same commit as the change that raised them. Either both land or neither
/// does; there is no window in which an event describes a write that was rolled
/// back, and none in which a write happens with its event lost.
/// <para>
/// Nothing is published here. Delivery is a separate concern with separate
/// failure modes — see <c>OutboxProcessor</c> — and doing it inside a
/// transaction would hold that transaction open across a network call.
/// </para>
/// </remarks>
internal sealed class DomainEventInterceptor : SaveChangesInterceptor
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Drain(eventData?.Context);
        return base.SavingChangesAsync(eventData!, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        Drain(eventData?.Context);
        return base.SavingChanges(eventData!, result);
    }

    private static void Drain(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var roots = context.ChangeTracker
            .Entries<AggregateRoot>()
            .Where(entry => entry.Entity.DomainEvents.Count > 0)
            .Select(entry => entry.Entity)
            .ToList();

        foreach (var root in roots)
        {
            foreach (var domainEvent in root.DomainEvents)
            {
                context.Set<OutboxMessage>().Add(new OutboxMessage
                {
                    // The event's own id, not a new one, so a redelivery is
                    // recognisable as the same event by whoever consumes it.
                    Id = domainEvent.EventId,

                    // The full type name, not the assembly-qualified one. The
                    // latter bakes an assembly version into stored data, and a
                    // message written last month then fails to deserialise
                    // after a routine bump.
                    Type = domainEvent.GetType().FullName!,
                    Payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), Options),
                    OccurredAt = domainEvent.OccurredAt,
                });
            }

            // Cleared after queueing, so a second SaveChanges on the same
            // tracked instance does not enqueue everything twice.
            root.ClearDomainEvents();
        }
    }
}
