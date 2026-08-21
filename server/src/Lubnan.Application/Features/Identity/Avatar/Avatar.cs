using System.Globalization;
using Lubnan.Application.Abstractions;
using Lubnan.Application.Abstractions.Http;
using Lubnan.Application.Abstractions.Messaging;
using Lubnan.Application.Abstractions.Security;
using Lubnan.Domain.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using DomainAvatar = Lubnan.Domain.Users.Avatar;

namespace Lubnan.Application.Features.Identity.Avatar;

// ── Upload ──────────────────────────────────────────────────────────────────

public sealed record SetAvatarCommand(byte[] Upload) : ICommand<Result<string>>;

internal sealed class SetAvatarHandler(
    IAppDbContext db,
    ICurrentUser currentUser,
    IImageSanitiser images,
    IClock clock)
    : ICommandHandler<SetAvatarCommand, Result<string>>
{
    public async Task<Result<string>> Handle(SetAvatarCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.Id is not { } userId)
        {
            return Result.Failure<string>(Error.Unauthorized("auth.required", "Sign in to continue."));
        }

        // Re-encoded before anything else happens, and before a single byte is
        // written anywhere. Nothing downstream ever handles the upload.
        var sanitised = await images
            .ToAvatarAsync(command.Upload, DomainAvatar.Size, cancellationToken)
            .ConfigureAwait(false);

        if (!sanitised.IsSuccess)
        {
            return Result.Failure<string>(Error.Validation("avatar.invalid", sanitised.Error!));
        }

        var now = clock.UtcNow;

        var existing = await db.Avatars
            .FirstOrDefaultAsync(a => a.UserId == userId, cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            var created = DomainAvatar.Create(userId, sanitised.Content, now);
            if (created.IsFailure)
            {
                return Result.Failure<string>(created.Error);
            }

            db.Avatars.Add(created.Value);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result.Success(created.Value.Version);
        }

        existing.Replace(sanitised.Content, now);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success(existing.Version);
    }
}

// ── Removal ─────────────────────────────────────────────────────────────────

public sealed record RemoveAvatarCommand : ICommand<Result>;

internal sealed class RemoveAvatarHandler(IAppDbContext db, ICurrentUser currentUser)
    : ICommandHandler<RemoveAvatarCommand, Result>
{
    public async Task<Result> Handle(RemoveAvatarCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.Id is not { } userId)
        {
            return Result.Failure(Error.Unauthorized("auth.required", "Sign in to continue."));
        }

        var existing = await db.Avatars
            .FirstOrDefaultAsync(a => a.UserId == userId, cancellationToken)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            db.Avatars.Remove(existing);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        // Success either way. Removing a picture that is not there is the
        // outcome that was asked for.
        return Result.Success();
    }
}

// ── Serving ─────────────────────────────────────────────────────────────────

public sealed record GetAvatarQuery(Guid UserId) : IQuery<Result<AvatarContent>>;

public sealed record AvatarContent(byte[] Content, string Version);

internal sealed class GetAvatarHandler(IAppDbContext db)
    : IQueryHandler<GetAvatarQuery, Result<AvatarContent>>
{
    public async Task<Result<AvatarContent>> Handle(GetAvatarQuery query, CancellationToken cancellationToken)
    {
        var avatar = await db.Avatars
            .AsNoTracking()
            .Where(a => a.UserId == query.UserId)
            .Select(a => new AvatarContent(
                a.Content,
                a.UpdatedAt.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return avatar is null
            ? Result.Failure<AvatarContent>(Error.NotFound("avatar.notFound", "No picture."))
            : Result.Success(avatar);
    }
}

internal sealed class Endpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/me/avatar", async (
                HttpRequest http,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                if (!http.HasFormContentType)
                {
                    return Results.Problem(
                        title: "Send the image as multipart/form-data.",
                        statusCode: StatusCodes.Status400BadRequest,
                        extensions: new Dictionary<string, object?> { ["code"] = "avatar.notMultipart" });
                }

                var form = await http.ReadFormAsync(cancellationToken);
                var file = form.Files["file"] ?? (form.Files.Count > 0 ? form.Files[0] : null);

                if (file is null || file.Length == 0)
                {
                    return Results.Problem(
                        title: "Choose an image first.",
                        statusCode: StatusCodes.Status400BadRequest,
                        extensions: new Dictionary<string, object?> { ["code"] = "avatar.missing" });
                }

                // Checked here as well as by the request size limit below,
                // because Length is what the client claimed. The limit on the
                // endpoint is what actually stops a large body arriving.
                if (file.Length > DomainAvatar.MaxUploadBytes)
                {
                    return Results.Problem(
                        title: $"That image is larger than {DomainAvatar.MaxUploadBytes / (1024 * 1024)} MB.",
                        statusCode: StatusCodes.Status400BadRequest,
                        extensions: new Dictionary<string, object?> { ["code"] = "avatar.tooLarge" });
                }

                using var buffer = new MemoryStream();
                await using (var stream = file.OpenReadStream())
                {
                    await stream.CopyToAsync(buffer, cancellationToken);
                }

                var result = await sender.Send(new SetAvatarCommand(buffer.ToArray()), cancellationToken);

                return result.IsSuccess
                    ? Results.Ok(new { version = result.Value })
                    : result.ToHttpResult();
            })
            .WithName("SetAvatar")
            .WithSummary("Upload a profile picture. Re-encoded server-side; the stored bytes are never the uploaded ones.")
            .WithTags("Identity")
            .ProducesValidationProblem()
            .RequireAuthorization()
            .RequireRateLimiting(RateLimits.Write)

            // The real ceiling, applied before the body is read.
            //
            // Without it Kestrel accepts its default 30 MB and the check inside
            // the handler measures a body that has already arrived and already
            // cost the memory. This refuses at the transport, which is the only
            // place a size limit means anything.
            .WithFormOptions(multipartBodyLengthLimit: DomainAvatar.MaxUploadBytes);

        app.MapDelete("/api/v1/me/avatar", async (ISender sender, CancellationToken cancellationToken) =>
                (await sender.Send(new RemoveAvatarCommand(), cancellationToken)).ToHttpResult())
            .WithName("RemoveAvatar")
            .WithSummary("Go back to initials.")
            .WithTags("Identity")
            .RequireAuthorization()
            .RequireRateLimiting(RateLimits.Write);

        // GET and HEAD, not MapGet.
        //
        // Minimal APIs map exactly the verb named, so HEAD answered 405 - and
        // HEAD is how the profile page asks "is there a picture" without
        // downloading one. The browser console filled with method-not-allowed
        // on a page that was working, which is the worst kind of error: loud,
        // harmless, and indistinguishable from a real one.
        app.MapMethods("/api/v1/users/{id:guid}/avatar", ["GET", "HEAD"], async (
                Guid id,
                HttpResponse response,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new GetAvatarQuery(id), cancellationToken);

                if (result.IsFailure)
                {
                    return result.ToHttpResult();
                }

                // Immutable, because the URL carries a version. A new picture is
                // a new URL, so the old one can be cached for a year without
                // ever showing a stale face.
                response.Headers.CacheControl = "public, max-age=31536000, immutable";
                response.Headers.ETag = $"\"{result.Value.Version}\"";

                // Content-Disposition inline with a filename we chose. Combined
                // with nosniff from the security headers, a browser has no
                // route to treating this as anything but an image.
                response.Headers.ContentDisposition = "inline; filename=\"avatar.webp\"";

                return Results.File(result.Value.Content, "image/webp");
            })
            .WithName("GetAvatar")
            .WithSummary("A user's profile picture, as WebP.")
            .WithTags("Identity")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .AllowAnonymous()
            .RequireRateLimiting(RateLimits.Read);
    }
}
