namespace Lubnan.Infrastructure.Persistence.Outbox;

/// <summary>How often the outbox is drained, and whether it runs at all.</summary>
public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    /// <summary>
    /// Off in the integration suite: those tests own the database and a
    /// background writer racing them is how a green suite becomes flaky.
    /// </summary>
    public bool Enabled { get; set; } = true;

    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// The ceiling the poll backs off to while the outbox stays empty.
    /// </summary>
    /// <remarks>
    /// Two seconds forever is ~43,000 queries a day against a database that
    /// usually has nothing to hand over. On a serverless plan that is not just
    /// waste: Neon suspends its compute after roughly five minutes idle and
    /// charges for the hours it is awake, so an unbroken poll keeps it awake
    /// permanently and spends a month's allowance in about a week.
    ///
    /// Thirty seconds lets the compute sleep. The cost is that a message
    /// written while the loop is at its slowest waits up to that long — which
    /// is a confirmation email arriving half a minute later, against a database
    /// that stops answering entirely.
    /// </remarks>
    public TimeSpan MaxPollInterval { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// After this many failures a message is left pending but skipped, so
    /// one unknown type cannot block the rest of the queue.
    /// </summary>
    public int MaxAttempts { get; set; } = 8;
}
