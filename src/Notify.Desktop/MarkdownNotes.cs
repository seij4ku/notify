using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Notify.Desktop;

static class MarkdownNotes
{
    public static string Save(string folder, string title, string url, string body)
    {
        Directory.CreateDirectory(folder);
        var safeTitle = Regex.Replace(title, @"\s*_\s*[A-Za-z]{2,}\d{3,}.*$", "").Replace("\r", " ").Replace("\n", " ").Trim();
        safeTitle = Regex.Replace(safeTitle, @"^Section\s+(\d+(?:\.\d+)*)\s+", "Section_$1_");
        var content = $"---\ntags:\n  - complete\ntag:\n---\n\nSource: [{url}]({url})\n\n{body.Trim()}\n";
        var name = Regex.Replace(safeTitle, "[<>:\"/\\\\|?*\\x00-\\x1F]", "_").Trim().TrimEnd('.');
        if (name.Length == 0) name = "Untitled";
        var temp = Path.Combine(folder, "." + Guid.NewGuid().ToString("N") + ".tmp");
        File.WriteAllText(temp, content, new UTF8Encoding(false));
        try
        {
            for (var i = 0; ; i++)
            {
                var dest = Path.Combine(folder, name + (i == 0 ? "" : $" ({i})") + ".md");
                try { File.Move(temp, dest); return dest; } catch (IOException) when (File.Exists(dest)) { }
            }
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
}
