using System.IO;
using System.Text;
using System.Text.Json;

namespace Notify.Desktop;

static class NativeHost
{
    public static void Run()
    {
        var input = Console.OpenStandardInput(); var output = Console.OpenStandardOutput();
        var header = new byte[4];
        while (true)
        {
            if (!ReadFully(input, header)) return;
            var length = BitConverter.ToInt32(header); if (length is < 2 or > 8_000_000) return;
            var payload = new byte[length]; if (!ReadFully(input, payload)) return;
            try
            {
                using var json = JsonDocument.Parse(payload); var root = json.RootElement;
                if (!root.TryGetProperty("version", out var version) || version.GetInt32() != 1 || !root.TryGetProperty("title", out var title) || !root.TryGetProperty("url", out var url) || !Uri.TryCreate(url.GetString(), UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https")) throw new InvalidDataException("Invalid capture message.");
                var folderFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Notify", "vault.txt");
                var folder = File.Exists(folderFile) ? File.ReadAllText(folderFile) : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "iCloudDrive", "iCloud~md~obsidian", "Obsidian Vault", "3. Rough Notes");
                var html = root.TryGetProperty("html", out var h) ? h.GetString() ?? "" : ""; var text = root.TryGetProperty("text", out var t) ? t.GetString() ?? "" : "";
                var captured = DateTime.Now; var path = MarkdownNotes.Save(folder, title.GetString() ?? "Untitled", uri.ToString(), html, text, captured);
                Reply(output, new { ok = true, path });
            }
            catch (Exception ex) { Reply(output, new { ok = false, error = ex.Message }); }
        }
    }
    static bool ReadFully(Stream stream, Span<byte> data) { var read = 0; while (read < data.Length) { var n = stream.Read(data[read..]); if (n == 0) return false; read += n; } return true; }
    static void Reply(Stream output, object value) { var bytes = JsonSerializer.SerializeToUtf8Bytes(value); output.Write(BitConverter.GetBytes(bytes.Length)); output.Write(bytes); output.Flush(); }
}
