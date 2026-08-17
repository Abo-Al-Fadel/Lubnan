using System.Text.Json;
using Lubnan.Application.Abstractions;
using Lubnan.Application.Abstractions.Security;
using Lubnan.Domain.Users;
using Lubnan.Domain.Users.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lubnan.Infrastructure.Persistence.Outbox;

/// <summary>
/// Delivers what <see cref="Interceptors.DomainEventInterceptor"/> queued.
/// </summary>
/// <remarks>
/// Delivery is a separate concern from the save on purpose. Mail is a network
/// call to a third party that can be slow or down, and doing it inside the
/// request would mean a registration that fails because a provider blinked —
/// after the account already existed.
/// <para>
/// At-least-once: a crash between send and ack redelivers. Confirmation tokens
/// are superseded on each attempt, so a second mail invalidates the first
/// link rather than leaving two live ways in.
/// </para>
/// </remarks>
internal sealed class OutboxProcessor(
    IServiceScopeFactory scopes,
    IOptions<OutboxOptions> options,
    ILogger<OutboxProcessor> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly OutboxOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DrainOnce(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.DrainFailed(exception);
            }

            try
            {
                await Task.Delay(_options.PollInterval, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
        }
    }

    private async Task DrainOnce(CancellationToken cancellationToken)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var mail = scope.ServiceProvider.GetRequiredService<IEmailSender>();
        var tokens = scope.ServiceProvider.GetRequiredService<ITokenFactory>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        var auth = scope.ServiceProvider.GetRequiredService<IOptions<AuthOptions>>().Value;

        var pending = await db.OutboxMessages
            .Where(m => m.ProcessedAt == null)
            .OrderBy(m => m.OccurredAt)
            .Take(20)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var message in pending)
        {
            try
            {
                await Dispatch(message, db, mail, tokens, clock, auth, cancellationToken)
                    .ConfigureAwait(false);
                message.ProcessedAt = clock.UtcNow;
                message.Error = null;
            }
            catch (Exception exception)
            {
                message.Attempts++;
                message.Error = exception.Message;
                logger.MessageFailed(exception, message.Id, message.Type);
            }
        }

        if (pending.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task Dispatch(
        OutboxMessage message,
        AppDbContext db,
        IEmailSender mail,
        ITokenFactory tokens,
        IClock clock,
        AuthOptions auth,
        CancellationToken cancellationToken)
    {
        if (message.Type == typeof(UserRegistered).FullName)
        {
            var evt = JsonSerializer.Deserialize<UserRegistered>(message.Payload, Json)
                      ?? throw new InvalidOperationException("UserRegistered payload did not deserialise.");
            await SendConfirmation(evt.UserId, evt.Email, evt.DisplayName, db, mail, tokens, clock, auth, cancellationToken)
                .ConfigureAwait(false);
            return;
        }

        if (message.Type == typeof(UserRegistrationReattempted).FullName)
        {
            var evt = JsonSerializer.Deserialize<UserRegistrationReattempted>(message.Payload, Json)
                      ?? throw new InvalidOperationException("UserRegistrationReattempted payload did not deserialise.");
            await mail.SendAsync(
                    new OutgoingEmail(
                        evt.Email,
                        "Someone tried to register with this address",
                        $"""
                        Someone just tried to create a Lubnān account with {evt.Email}.

                        If that was you, sign in instead:
                        {auth.WebBaseUrl.TrimEnd('/')}/login

                        If it was not you, you can ignore this. Nobody else can use this address to open an account.
                        """),
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private static async Task SendConfirmation(
        Guid userId,
        string email,
        string displayName,
        AppDbContext db,
        IEmailSender mail,
        ITokenFactory tokens,
        IClock clock,
        AuthOptions auth,
        CancellationToken cancellationToken)
    {
        var user = await db.Users
            .Include(u => u.Tokens)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            .ConfigureAwait(false);

        if (user is null || user.EmailConfirmed)
        {
            return;
        }

        var issued = tokens.CreatePurposeToken();
        user.IssueToken(TokenPurpose.ConfirmEmail, issued.Hash, clock.UtcNow, UserToken.ConfirmEmailLifetime);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var link = $"{auth.WebBaseUrl.TrimEnd('/')}/confirm-email?token={Uri.EscapeDataString(issued.Value)}";

        await mail.SendAsync(
                new OutgoingEmail(
                    email,
                    "Confirm your Lubnān account",
                    $"""
                    Hello {displayName},

                    Confirm this address to finish creating your account:
                    {link}

                    The link expires in three days. If you did not register, ignore this.
                    """),
                cancellationToken)
            .ConfigureAwait(false);
    }
}

internal static partial class OutboxProcessorMessages
{
    [LoggerMessage(EventId = 4100, Level = LogLevel.Error, Message = "Outbox drain failed")]
    public static partial void DrainFailed(this ILogger logger, Exception exception);

    [LoggerMessage(EventId = 4101, Level = LogLevel.Warning, Message = "Outbox message {MessageId} ({Type}) failed")]
    public static partial void MessageFailed(this ILogger logger, Exception exception, Guid messageId, string type);
}
