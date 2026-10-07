using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Notify.Desktop;

sealed record NoteResult(string Path, string Title)
{
    public string Label => $"{Title}    —    {Path}";
}

static class ResearchPack
{
    public static string[] GetTagOptions(string directory) => Directory.Exists(directory)
        ? Directory.EnumerateFiles(directory).Select(path => Path.GetFileNameWithoutExtension(path)).OfType<string>().OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray()
        : [];

    public static List<NoteResult> Search(string vault, string query, string tagFilter)
    {
        var tags = tagFilter.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var results = new List<NoteResult>();
        // ponytail: scans markdown on each search; add an index only if real vaults make this slow.
        foreach (var path in Directory.EnumerateFiles(vault, "*.md", new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true, AttributesToSkip = FileAttributes.ReparsePoint }))
        {
            try
            {
                var content = File.ReadAllText(path);
                var title = Path.GetFileNameWithoutExtension(path);
                if (!(title.Contains(query, StringComparison.OrdinalIgnoreCase) || content.Contains(query, StringComparison.OrdinalIgnoreCase)) || !tags.All(ReadTags(content).Contains)) continue;
                results.Add(new NoteResult(path, title));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
        }
        return results;
    }

    public static string Create(string root, string name, IReadOnlyList<NoteResult> notes)
    {
        if (notes.Count == 0) throw new InvalidOperationException("Select at least one note for the research pack.");
        Directory.CreateDirectory(root);
        var stem = Regex.Replace(name.Trim(), "[<>:\"/\\\\|?*\\x00-\\x1F]", "_").Trim().TrimEnd('.');
        if (stem.Length == 0) stem = "Research Pack";
        for (var i = 0; ; i++)
        {
            var folder = Path.Combine(root, stem + (i == 0 ? "" : $" ({i})"));
            if (Directory.Exists(folder)) continue;
            Directory.CreateDirectory(folder);
            WritePack(folder, stem, notes);
            return folder;
        }
    }

    static void WritePack(string folder, string name, IReadOnlyList<NoteResult> notes)
    {
        var markdown = new StringBuilder($"# {name}\n\nCompiled from {notes.Count} selected Obsidian notes.\n");
        foreach (var note in notes)
        {
            markdown.Append("\n---\n\n## ").Append(note.Title).Append("\n\nSource: `").Append(note.Path).Append("`\n\n").AppendLine(File.ReadAllText(note.Path).Trim());
        }
        File.WriteAllText(Path.Combine(folder, name + ".md"), markdown.ToString(), new UTF8Encoding(false));
    }

    static HashSet<string> ReadTags(string markdown)
    {
        var tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase); var lines = markdown.Split('\n');
        if (lines.Length == 0 || lines[0].Trim() != "---") return tags;
        var inTags = false;
        foreach (var raw in lines.Skip(1))
        {
            var line = raw.Trim(); if (line == "---") break;
            if (line.StartsWith("tags:", StringComparison.OrdinalIgnoreCase))
            {
                inTags = true; AddInline(line[5..], tags); continue;
            }
            if (line.StartsWith("tag:", StringComparison.OrdinalIgnoreCase))
            {
                inTags = false; AddInline(line[4..], tags); continue;
            }
            if (inTags && line.StartsWith("-", StringComparison.Ordinal)) AddInline(line[1..], tags); else if (!raw.StartsWith(' ') && !raw.StartsWith('\t')) inTags = false;
        }
        return tags;
    }

    static void AddInline(string value, HashSet<string> tags)
    {
        foreach (var tag in value.Trim().Trim('[', ']').Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var clean = tag.Trim('"', '\''); if (clean.Length > 0) tags.Add(clean);
        }
    }
}
