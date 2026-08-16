using System.Globalization;
using Lubnan.Application.Abstractions;
using Microsoft.Extensions.Logging;

// Namespace "Mail" rather than "Email", because Lubnan.Infrastructure.Email
// would shadow the Email value object everywhere in this assembly that both are
// in scope — and the error it produces, "Email is a namespace but is used like
// a type", points at the innocent file rather than at this one.
namespace Lubnan.Infrastructure.Mail;

/// <summary>
/// Writes mail to disk instead of sending it. For development.
/// </summary>
/// <remarks>
/// A registration flow cannot be built or tested without seeing the message,
/// and the alternatives are worse: a real provider in development eventually
/// sends a test message to a real stranger, and an SMTP catcher is another
/// container to run.
/// <para>
/// The confirmation link is logged as well as written, because the thing a
/// developer actually wants is to click it.
/// </para>
/// </remarks>
internal sealed class FileEmailSender(ILogger<FileEmailSender> logger) : IEmailSender
{
    private static readonly string Directory =
        Path.Combine(Path.GetTempPath(), "lubnan-mail");

    public async Task SendAsync(OutgoingEmail email, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(email);

        System.IO.Directory.CreateDirectory(Directory);

        var name = $"{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}-{Sanitise(email.To)}.txt";
        var path = Path.Combine(Directory, name);

        await File.WriteAllTextAsync(
            path,
            string.Create(
                CultureInfo.InvariantCulture,
                $"To: {email.To}\nSubject: {email.Subject}\n\n{email.Body}\n"),
            cancellationToken).ConfigureAwait(false);

        logger.WroteMail(email.To, email.Subject, path);
    }

    // The address is part of a filename. Anything path-shaped in it would let a
    // registration write outside the directory.
    private static string Sanitise(string address) =>
        string.Concat(address.Select(c => char.IsAsciiLetterOrDigit(c) ? c : '_'));
}

internal static partial class FileEmailSenderMessages
{
    [LoggerMessage(
        EventId = 4000,
        Level = LogLevel.Information,
        Message = "Mail for {To} ({Subject}) written to {Path}")]
    public static partial void WroteMail(this ILogger logger, string to, string subject, string path);
}
