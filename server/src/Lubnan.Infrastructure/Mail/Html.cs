using System.Net;
using System.Text;

namespace Lubnan.Infrastructure.Mail;

/// <summary>
/// Turns the plain-text body of a message into a minimal HTML part.
/// </summary>
/// <remarks>
/// The templates stay plain text, which is the right place for them: they are
/// short, they are read as prose in the source, and nobody should be editing
/// markup to change a sentence. This derives the HTML part from that text
/// instead of asking every call site to write both.
/// <para>
/// Deliberately minimal — paragraphs, line breaks and links, no styling. A mail
/// client's default rendering of a plain document is reliable across all of
/// them; a designed one is a week of testing against clients that each support
/// a different decade of CSS.
/// </para>
/// </remarks>
internal static class Html
{
    public static string FromPlainText(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return string.Empty;
        }

        var html = new StringBuilder("<div style=\"font-family:system-ui,-apple-system,sans-serif;line-height:1.6\">");

        // A blank line separates paragraphs; a single newline is a break within
        // one. That is the convention the templates are already written in.
        foreach (var paragraph in body.Replace("\r\n", "\n", StringComparison.Ordinal)
                     .Split("\n\n", StringSplitOptions.RemoveEmptyEntries))
        {
            html.Append("<p>");

            var lines = paragraph.Split('\n');

            for (var i = 0; i < lines.Length; i++)
            {
                if (i > 0)
                {
                    html.Append("<br>");
                }

                html.Append(Linkify(lines[i].Trim()));
            }

            html.Append("</p>");
        }

        return html.Append("</div>").ToString();
    }

    /// <summary>
    /// Escape first, then wrap anything that starts with https:// in an anchor.
    /// </summary>
    /// <remarks>
    /// The order matters and is the whole security of this function. Escaping
    /// after building the anchor would escape the markup we just wrote;
    /// escaping before means the URL is already inert by the time it is placed
    /// in an href, so a hostile display name cannot close the attribute and
    /// open a script tag.
    /// </remarks>
    private static string Linkify(string line)
    {
        var escaped = WebUtility.HtmlEncode(line);

        if (!escaped.StartsWith("https://", StringComparison.Ordinal))
        {
            return escaped;
        }

        // The whole line is the URL - that is how the templates lay links out,
        // on a line of their own - so there is nothing to split.
        return $"<a href=\"{escaped}\" style=\"color:#1d6f7a\">{escaped}</a>";
    }
}
