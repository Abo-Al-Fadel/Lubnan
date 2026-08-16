using Lubnan.Application.Abstractions;
using Lubnan.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Lubnan.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Stamps <c>created_at</c> and <c>updated_at</c> on every entity that has
/// them, on the way out.
/// </summary>
/// <remarks>
/// An interceptor rather than a database trigger, because a trigger is
/// invisible from the code and the first person to debug a mystery timestamp
/// has to know to go looking in the schema. It is also not a base-class
/// property, because "when was this row written" is a fact about the row and
/// not about the place, and putting it on the entity invites a handler to make
/// a decision from it.
/// <para>
/// The times come from <see cref="IClock"/>, so a test can save at a chosen
/// instant and assert on it.
/// </para>
/// </remarks>
internal sealed class AuditInterceptor(IClock clock) : SaveChangesInterceptor
{
    private const string CreatedAt = "CreatedAt";
    private const string UpdatedAt = "UpdatedAt";

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Stamp(eventData?.Context);
        return base.SavingChangesAsync(eventData!, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        Stamp(eventData?.Context);
        return base.SavingChanges(eventData!, result);
    }

    private void Stamp(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = clock.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries<Entity>())
        {
            if (entry.State is EntityState.Added)
            {
                Set(entry, CreatedAt, now);
                Set(entry, UpdatedAt, now);
            }
            else if (entry.State is EntityState.Modified)
            {
                // Coordinates and Plates are complex properties, not owned
                // entities, so a change inside one marks the parent Modified
                // and lands here. That is the second reason to prefer complex
                // types: with an owned reference the parent stays Unchanged and
                // moving a place would not touch its updated_at.
                Set(entry, UpdatedAt, now);
            }
        }
    }

    // Not every entity declares the shadow properties — the outbox has its own
    // notion of time — so ask rather than assume.
    private static void Set(EntityEntry entry, string property, DateTimeOffset value)
    {
        if (entry.Metadata.FindProperty(property) is not null)
        {
            entry.Property(property).CurrentValue = value;
        }
    }
}
