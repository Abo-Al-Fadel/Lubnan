namespace Lubnan.Application.Abstractions.Security;

/// <summary>What came back from trying to turn an upload into an avatar.</summary>
/// <param name="Content">WebP bytes we produced, or empty on failure.</param>
/// <param name="Error">Null on success; a reader-facing sentence otherwise.</param>
public readonly record struct SanitisedImage(byte[] Content, string? Error)
{
    public bool IsSuccess => Error is null;

    public static SanitisedImage Ok(byte[] content) => new(content, null);

    public static SanitisedImage Failed(string error) => new([], error);
}

/// <summary>
/// Turns an uploaded file into an image we generated.
/// </summary>
/// <remarks>
/// The contract is deliberately narrow: bytes in, bytes out, and the bytes that
/// come out are never the bytes that went in. An implementation that validated
/// the upload and returned it unchanged would satisfy the signature and defeat
/// the purpose.
/// <para>
/// It must reject rather than throw for anything a person could plausibly
/// upload — the wrong file type, a corrupt file, something enormous. Those are
/// ordinary mistakes with an ordinary answer, not exceptional conditions.
/// </para>
/// </remarks>
public interface IImageSanitiser
{
    /// <param name="upload">The raw bytes, untrusted in every respect.</param>
    /// <param name="size">Output edge length in pixels. Square.</param>
    Task<SanitisedImage> ToAvatarAsync(
        byte[] upload,
        int size,
        CancellationToken cancellationToken = default);
}
