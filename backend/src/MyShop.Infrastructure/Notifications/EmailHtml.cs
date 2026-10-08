using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace MyShop.Infrastructure.Notifications;

internal static partial class EmailHtml
{
    internal static string FromPlainText(string body)
    {
        var html = new StringBuilder("<!doctype html><html><body>");
        var position = 0;
        foreach (Match match in Links().Matches(body))
        {
            html.Append(Encode(body[position..match.Index]));
            var encoded = WebUtility.HtmlEncode(match.Value);
            html.Append("<a href=\"").Append(encoded).Append("\">").Append(encoded).Append("</a>");
            position = match.Index + match.Length;
        }
        return html.Append(Encode(body[position..])).Append("</body></html>").ToString();
    }

    private static string Encode(string text) => WebUtility.HtmlEncode(text)
        .Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "<br>\n");

    [GeneratedRegex("https?://[^\\s<>\\\"]+", RegexOptions.IgnoreCase)]
    private static partial Regex Links();
}
