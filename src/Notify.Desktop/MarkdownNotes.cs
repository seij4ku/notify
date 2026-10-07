using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Notify.Desktop;

static class MarkdownNotes
{
    public static string Save(string folder, string title, string url, string html, string text, DateTime captured)
    {
        Directory.CreateDirectory(folder);
        var body = html.Length > 0 ? HtmlToMarkdown(html) : text;
        var safeTitle = title.Replace("\r", " ").Replace("\n", " ").Trim();
        var content = $"---\ndate: {captured:yyyy-MM-dd HH:mm:ss}\ntags:\n  - complete\ntag:\n---\n\n# {safeTitle}\n\nSource: [{url}]({url})\n\n{body.Trim()}\n";
        var name = Regex.Replace(safeTitle, "[<>:\"/\\\\|?*\\x00-\\x1F]", "_").Trim().TrimEnd('.');
        if (name.Length == 0) name = "Untitled";
        var stem = $"{captured:yyyy-MM-dd HHmmss} {name}"; var temp = Path.Combine(folder, "." + Guid.NewGuid().ToString("N") + ".tmp");
        File.WriteAllText(temp, content, new UTF8Encoding(false));
        try
        {
            for (var i = 0; ; i++)
            {
                var dest = Path.Combine(folder, stem + (i == 0 ? "" : $" ({i})") + ".md");
                try { File.Move(temp, dest); return dest; } catch (IOException) when (File.Exists(dest)) { }
            }
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
    static string HtmlToMarkdown(string html)
    {
        html = Regex.Replace(html, "<(script|style|nav|header|footer|aside)[^>]*>[\\s\\S]*?</\\1>", "", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, "<h([1-6])[^>]*>", m => "\n\n" + new string('#', int.Parse(m.Groups[1].Value)) + " ", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, "</h[1-6]>", "\n\n", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, "<li[^>]*>", "\n- ", RegexOptions.IgnoreCase); html = Regex.Replace(html, "</li>", "", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, "<br\\s*/?>|</p>|</div>|</section>|</article>", "\n\n", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, "<a[^>]*href=[\"']([^\"']+)[\"'][^>]*>(.*?)</a>", "[$2]($1)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        html = Regex.Replace(html, "</?(strong|b)>", "**", RegexOptions.IgnoreCase); html = Regex.Replace(html, "</?(em|i)>", "*", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, "<[^>]+>", " ");
        return WebUtility.HtmlDecode(Regex.Replace(html, "[ \\t]+", " ")).Trim();
    }
}
