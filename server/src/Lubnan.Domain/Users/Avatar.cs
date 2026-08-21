using Lubnan.Domain.Common;

namespace Lubnan.Domain.Users;

/// <summary>
/// A profile picture, after it has been re-encoded by us.
/// </summary>
/// <remarks>
/// <b>These bytes are never the bytes that were uploaded.</b> The upload is
/// decoded, resized and written out again by the server, and only the result
/// reaches this type. That single decision is most of the security of the
/// feature, and it is worth being explicit about why.
/// <para>
/// A file that a browser will render is a file that can attack the person
/// viewing it. The classic shapes are a polyglot — bytes that are a valid JPEG
/// <em>and</em> a valid HTML document, so a browser that sniffs the type runs
/// the script half — and a payload smuggled in an EXIF comment that some
/// downstream tool later interprets. Neither survives being decoded to a pixel
/// buffer and encoded again: what comes out is a picture we generated, with no
/// metadata, in a format we chose.
/// </para>
/// <para>
/// Scanning for known malware signatures would be strictly weaker. It answers
/// "is this one of the bad files I have seen before", where re-encoding answers
/// "is this a picture", which is the question actually being asked.
/// </para>
/// <para>
/// A separate entity rather than columns on <see cref="User"/>, so that loading
/// a user to check a password does not drag an image out of the database with
/// it. Nothing in the sign-in path touches this table.
/// </para>
/// </remarks>
public sealed class Avatar : Entity
{
    /// <summary>
    /// Both dimensions, in pixels. Square, because every surface that shows one
    /// shows it in a circle or a square, and cropping once here beats cropping
    /// in six different components.
    /// </summary>
    public const int Size = 256;

    /// <summary>
    /// The largest upload accepted, before decoding.
    /// </summary>
    /// <remarks>
    /// Checked against the request length and again while reading, because a
    /// declared length is a claim. Four megabytes is generous for something
    /// that will be stored at a couple of dozen kilobytes; the point of the cap
    /// is to bound the work, not to be tight.
    /// </remarks>
    public const int MaxUploadBytes = 4 * 1024 * 1024;

    private Avatar(Guid id, Guid userId, byte[] content, DateTimeOffset updatedAt) : base(id)
    {
        UserId = userId;
        Content = content;
        UpdatedAt = updatedAt;
    }

    private Avatar() { }

    public Guid UserId { get; private init; }

    /// <summary>WebP, always, because we encoded it.</summary>
    public byte[] Content { get; private set; } = [];

    /// <summary>
    /// Changes on every replacement, and appears in the URL the page requests.
    /// </summary>
    /// <remarks>
    /// The image is served with a long immutable cache — it is a hundred
    /// requests a day otherwise — which means a new picture would not appear
    /// until the old one expired. A version in the query string makes the new
    /// one a different URL, so it is fetched immediately and the old one ages
    /// out on its own.
    /// </remarks>
    public DateTimeOffset UpdatedAt { get; private set; }

    public string Version => UpdatedAt.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture);

    public static Result<Avatar> Create(Guid userId, byte[] content, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (content.Length == 0)
        {
            return Result.Failure<Avatar>(Error.Validation(
                "avatar.empty", "That image could not be read."));
        }

        return Result.Success(new Avatar(Guid.NewGuid(), userId, content, now));
    }

    public void Replace(byte[] content, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(content);

        Content = content;
        UpdatedAt = now;
    }
}
