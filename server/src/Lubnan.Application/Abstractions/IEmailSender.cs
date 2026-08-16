namespace Lubnan.Application.Abstractions;

/// <summary>One message, already rendered.</summary>
public sealed record OutgoingEmail(string To, string Subject, string Body);

/// <summary>
/// Delivers mail. The interface is deliberately this small.
/// </summary>
/// <remarks>
/// Nothing in the application knows which provider sends the message, and the
/// choice of one is not allowed to block writing the flows that need it. In
/// development the implementation writes files; in production it posts to
/// whichever API is configured, and no handler changes.
/// <para>
/// Sending happens from the outbox, not from the handler. Mail delivery is a
/// network call to a third party that can be slow or down, and doing it inside
/// the request means a registration that fails because a mail server was
/// unreachable — creating the account and then reporting failure. Raising a
/// domain event puts the message on a queue that retries.
/// </para>
/// </remarks>
public interface IEmailSender
{
    Task SendAsync(OutgoingEmail email, CancellationToken cancellationToken = default);
}
