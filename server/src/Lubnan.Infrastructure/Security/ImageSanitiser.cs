using Lubnan.Application.Abstractions.Security;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace Lubnan.Infrastructure.Security;

/// <summary>
/// Decodes an upload and writes a new picture from the pixels.
/// </summary>
/// <remarks>
/// ImageSharp rather than SkiaSharp or a shell out to ImageMagick, for one
/// practical reason: it is entirely managed code. The runtime image is Alpine,
/// and every native imaging library wants a set of musl-compatible shared
/// objects that then have to be tracked for their own CVEs. A pure managed
/// decoder is one dependency in one place.
/// </remarks>
internal sealed class ImageSanitiser(ILogger<ImageSanitiser> logger) : IImageSanitiser
{
    /// <summary>
    /// Decoded dimensions we refuse to work with.
    /// </summary>
    /// <remarks>
    /// A decompression bomb is a small file that decodes enormous: a few
    /// kilobytes of PNG can describe 50,000 x 50,000 pixels, which is roughly
    /// ten gigabytes of buffer. The upload cap does not stop it, because the
    /// file really is tiny — the size has to be checked after reading the
    /// header and before decoding the body.
    /// </remarks>
    private const int MaxDimension = 12_000;

    public async Task<SanitisedImage> ToAvatarAsync(
        byte[] upload,
        int size,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(upload);

        try
        {
            // Read the header alone first. Identify parses metadata without
            // allocating a pixel buffer, so a bomb is refused before it costs
            // anything.
            using var probe = new MemoryStream(upload, writable: false);
            var info = await Image.IdentifyAsync(probe, cancellationToken).ConfigureAwait(false);

            if (info.Width > MaxDimension || info.Height > MaxDimension)
            {
                return SanitisedImage.Failed(
                    $"That image is {info.Width}x{info.Height}. The largest accepted is {MaxDimension} on a side.");
            }

            probe.Position = 0;

            // Decoding is where an upload stops being a file and becomes
            // pixels. Everything that made it dangerous - a second format
            // hiding after the first, a script in an EXIF comment, a trailing
            // archive - is not pixels, and does not survive.
            using var image = await Image.LoadAsync(probe, cancellationToken).ConfigureAwait(false);

            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(size, size),

                // Crop rather than letterbox: every surface shows this in a
                // circle, and a letterboxed portrait becomes a face with bars
                // through it.
                Mode = ResizeMode.Crop,
                Position = AnchorPositionMode.Center,
            }));

            // Metadata is dropped wholesale rather than filtered. EXIF carries
            // GPS coordinates on nearly every phone photograph, and somebody
            // uploading a profile picture is not consenting to publish where
            // they took it.
            image.Metadata.ExifProfile = null;
            image.Metadata.IptcProfile = null;
            image.Metadata.XmpProfile = null;

            var output = new MemoryStream();

            await image.SaveAsync(
                output,
                new WebpEncoder { Quality = 82, FileFormat = WebpFileFormatType.Lossy },
                cancellationToken).ConfigureAwait(false);

            return SanitisedImage.Ok(output.ToArray());
        }
        catch (Exception ex) when (ex is UnknownImageFormatException or InvalidImageContentException)
        {
            // The two ordinary failures: not a picture, or a broken one. Both
            // are a person's mistake rather than an incident, so they answer
            // with a sentence rather than a stack trace.
            return SanitisedImage.Failed("That file is not an image we can read. Try a JPEG, PNG or WebP.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Anything else is worth knowing about, and still must not become a
            // 500 on a profile page.
            logger.SanitiseFailed(ex);
            return SanitisedImage.Failed("That image could not be processed.");
        }
    }
}

internal static partial class ImageSanitiserLog
{
    [LoggerMessage(EventId = 4400, Level = LogLevel.Warning, Message = "An upload could not be re-encoded.")]
    public static partial void SanitiseFailed(this ILogger logger, Exception exception);
}
