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
}
