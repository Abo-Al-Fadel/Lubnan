using Lubnan.Domain.Common;

namespace Lubnan.Domain.Places;

/// <summary>
/// The image plates a place uses, by id rather than by URL.
/// </summary>
/// <remarks>
/// The API returns <c>"J1"</c>, never <c>"/img/J/J1.png"</c>. The frontend
/// already resolves ids to paths in <c>lib/plates.ts</c>, including the phone
/// crop probe and the extension chain, and it is the half that knows the
/// viewport. Returning a path from the server would freeze a CDN host, a file
/// extension and a directory layout into every stored row.
/// <para>
/// Every plate is optional. The site is built to render with zero, some or all
/// plates present, and the API must not pretend otherwise.
/// </para>
/// </remarks>
public sealed class PlateSet : ValueObject
{
    public static readonly PlateSet Empty = new(null, null, null, null, null);

    /// <summary>EF materialises through this; nothing else should.</summary>
    private PlateSet() { }

    private PlateSet(string? hero, string? frame, string? subject, string? rail, string? mosaic)
    {
        Hero = hero;
        Frame = frame;
        Subject = subject;
        Rail = rail;
        Mosaic = mosaic;
    }

    /// <summary>Full-bleed banner on the place page.</summary>
    public string? Hero { get; }

    /// <summary>The annotated plate the callouts are positioned against.</summary>
    public string? Frame { get; }

    /// <summary>Alpha cut-out for the type-behind-subject treatment.</summary>
    public string? Subject { get; }

    /// <summary>Card in the horizontal rail.</summary>
    public string? Rail { get; }

    /// <summary>Card in the mosaic grid.</summary>
    public string? Mosaic { get; }

    public static PlateSet Create(
        string? hero = null,
        string? frame = null,
        string? subject = null,
        string? rail = null,
        string? mosaic = null) =>
        new(Clean(hero), Clean(frame), Clean(subject), Clean(rail), Clean(mosaic));

    // An empty string and a null both mean "no plate", and storing both means
    // every reader has to test for both. Collapse at the boundary.
    private static string? Clean(string? id) =>
        string.IsNullOrWhiteSpace(id) ? null : id.Trim();

    protected override IEnumerable<object?> GetEqualityComponents() =>
        [Hero, Frame, Subject, Rail, Mosaic];
}
