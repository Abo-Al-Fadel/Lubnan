using Lubnan.Application.Abstractions;
using Lubnan.Application.Features.Identity;
using Lubnan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lubnan.Infrastructure.Jobs;

public sealed class PurgeOptions
{
    public const string SectionName = "Purge";

    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Hourly. The grace period is thirty days, so the difference between
    /// purging at 09:00 and at 09:59 on the last day is nothing anybody can
    /// perceive, and a slow sweep costs a database round trip an hour.
    /// </summary>
    public TimeSpan Interval { get; set; } = TimeSpan.FromHours(1);

    /// <summary>How many accounts one pass will anonymise.</summary>
    public int BatchSize { get; set; } = 50;
}

/// <summary>
/// Anonymises accounts whose grace period has run out.
/// </summary>
/// <remarks>
/// This is the only thing in the system that can reach
/// <see cref="Domain.Users.AccountState.Anonymised"/>, and that is the point.
/// No HTTP request destroys data; a request can only start a clock, and the
/// clock is read here.
/// <para>
/// The work itself is <c>User.Anonymise</c> — this class decides only
/// <em>when</em>. It re-checks the deadline through the domain rather than
/// trusting its own query, so a race between the query and the save (somebody
/// cancelling their deletion in that window) is refused by the aggregate rather
/// than winning.
/// </para>
/// </remarks>
public sealed class AccountPurgeWorker(
    IServiceScopeFactory scopes,
    IOptions<PurgeOptions> options,
    IClock clock,
    ILogger<AccountPurgeWorker> logger) : BackgroundService
{
    private readonly PurgeOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.PurgeDisabled();
            return;
        }

        // A short stagger before the first pass. Several replicas starting
        // together would otherwise all sweep at the same instant, and while the
        // domain makes that harmless it makes the logs unreadable.
        await Task.Delay(TimeSpan.FromSeconds(Random.Shared.Next(5, 30)), stoppingToken).ConfigureAwait(false);

        using var timer = new PeriodicTimer(_options.Interval);

        do
        {
            try
            {
                await SweepAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Never let one bad pass kill the worker. An unhandled exception
                // in a BackgroundService stops it silently for the lifetime of
                // the process, so the next thirty days of deletions would
                // quietly not happen.
                logger.PurgeFailed(ex);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
    }

    private async Task SweepAsync(CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;

        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tombstoner = scope.ServiceProvider.GetRequiredService<IEmailTombstoner>();

        // Matches the partial index: purge_after IS NOT NULL AND anonymised_at
        // IS NULL. The queue stays the size of the backlog rather than of the
        // user table.
        var due = await db.Users
            .Where(u => u.PurgeAfter != null && u.AnonymisedAt == null && u.PurgeAfter <= now)
            .OrderBy(u => u.PurgeAfter)
            .Take(_options.BatchSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (due.Count == 0)
        {
            return;
        }

        var purged = 0;

        foreach (var user in due)
        {
            var tombstone = tombstoner.Tombstone(user.Email.Value);

            // Anonymise re-checks the state and the deadline. If somebody
            // cancelled their deletion between the query above and this line,
            // it refuses and the account survives — which is the outcome that
            // should win a race against an irreversible operation.
            var result = user.Anonymise(tombstone, clock.UtcNow);

            if (result.IsFailure)
            {
                logger.PurgeSkipped(user.Id, result.Error.Code);
                continue;
            }

            purged++;
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.Purged(purged, due.Count);
    }
}

internal static partial class PurgeLog
{
    [LoggerMessage(EventId = 4200, Level = LogLevel.Warning,
        Message = "Account purge worker is disabled. Accounts past their grace period will not be anonymised.")]
    public static partial void PurgeDisabled(this ILogger logger);

    [LoggerMessage(EventId = 4201, Level = LogLevel.Information,
        Message = "Purge pass complete: {Purged} of {Considered} accounts anonymised.")]
    public static partial void Purged(this ILogger logger, int purged, int considered);

    [LoggerMessage(EventId = 4202, Level = LogLevel.Information,
        Message = "Account {UserId} was not anonymised: {Code}")]
    public static partial void PurgeSkipped(this ILogger logger, Guid userId, string code);

    [LoggerMessage(EventId = 4203, Level = LogLevel.Error, Message = "Account purge pass failed.")]
    public static partial void PurgeFailed(this ILogger logger, Exception exception);
}
