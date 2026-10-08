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

    public static List<NoteResult> Search(string vault, string query, string tagFilter, string tagDirectory)
    {
        var tags = tagFilter.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var results = new List<NoteResult>();
        var files = Directory.EnumerateFiles(vault, "*.md", new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true, AttributesToSkip = FileAttributes.ReparsePoint }).ToArray();
        HashSet<string>? allowed = null;
        foreach (var tag in tags)
        {
            var template = Directory.Exists(tagDirectory)
                ? Directory.EnumerateFiles(tagDirectory).FirstOrDefault(path => string.Equals(Path.GetFileNameWithoutExtension(path), tag, StringComparison.OrdinalIgnoreCase))
                : null;
            if (template is null) throw new FileNotFoundException($"No tag template named '{tag}' was found in the configured tag directory.");
            var tagFiles = ReadTemplateFiles(File.ReadAllText(template), vault, tagDirectory, files);
            if (allowed is null) allowed = tagFiles; else allowed.IntersectWith(tagFiles);
        }
        // ponytail: scans markdown on each search; add an index only if real vaults make this slow.
        foreach (var path in files)
        {
            try
            {
                if (allowed is not null && !allowed.Contains(Path.GetFullPath(path))) continue;
                var content = File.ReadAllText(path);
                var title = Path.GetFileNameWithoutExtension(path);
                if (!(title.Contains(query, StringComparison.OrdinalIgnoreCase) || content.Contains(query, StringComparison.OrdinalIgnoreCase))) continue;
                results.Add(new NoteResult(path, title));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
        }
        return results;
    }

    static HashSet<string> ReadTemplateFiles(string markdown, string vault, string tagDirectory, string[] vaultFiles)
    {
        var byPath = vaultFiles.ToDictionary(path => Path.GetRelativePath(vault, path).Replace('\\', '/')[..^3], StringComparer.OrdinalIgnoreCase);
        var byTitle = vaultFiles.GroupBy(path => Path.GetFileNameWithoutExtension(path) ?? "", StringComparer.OrdinalIgnoreCase).ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.OrdinalIgnoreCase);
        var matches = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var lines = markdown.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            if (!lines[i].Contains('|')) continue;
            var rows = new List<string[]>();
            while (i < lines.Length && lines[i].Contains('|')) rows.Add(SplitTableRow(lines[i++]));
            i--;
            var firstDataRow = rows.Count > 1 && IsSeparatorRow(rows[1]) ? 2 : 0;
            var fileColumns = firstDataRow == 2
                ? rows[0].Select((header, index) => (header, index)).Where(item => Regex.IsMatch(item.header.Trim().Trim('*').ToLowerInvariant(), @"(^|\s)(file|files|filename|note|notes|path|page)(\s|$)")).Select(item => item.index).ToArray()
                : Array.Empty<int>();
            foreach (var row in rows.Skip(firstDataRow))
                foreach (var cell in (fileColumns.Length == 0 ? row : fileColumns.Where(index => index < row.Length).Select(index => row[index])))
                {
                    var links = Regex.Matches(cell, @"\[\[([^\]]+)\]\]|\[[^\]]+\]\(([^)]+)\)");
                    if (links.Count == 0) AddResolved(cell, matches, byPath, byTitle);
                    foreach (Match link in links)
                    {
                        var target = link.Groups[1].Success ? link.Groups[1].Value : link.Groups[2].Value;
                        AddResolved(target, matches, byPath, byTitle);
                    }
                }
        }
        if (matches.Count == 0 && HasDynamicTagBase(markdown, vault, tagDirectory))
        {
            var tags = ReadFrontmatterValues(markdown, "tag|tags").Select(NormalizeTag).Where(tag => tag.Length > 0).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var path in vaultFiles)
            {
                try
                {
                    if (ReadFrontmatterValues(File.ReadAllText(path), "tag|tags").Select(NormalizeTag).Any(tags.Contains))
                        matches.Add(Path.GetFullPath(path));
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
            }
        }
        return matches;
    }

    static bool HasDynamicTagBase(string markdown, string vault, string tagDirectory)
    {
        foreach (Match embed in Regex.Matches(markdown, @"!\[\[([^\]|#]+\.base)(?:[|#][^\]]*)?\]\]", RegexOptions.IgnoreCase))
        {
            var name = Path.GetFileName(embed.Groups[1].Value.Trim());
            var candidates = new[] { Path.Combine(vault, name), Path.Combine(tagDirectory, name) };
            foreach (var candidate in candidates)
                if (File.Exists(candidate) && Regex.IsMatch(File.ReadAllText(candidate), @"list\(tag\)\.contains\(this\)", RegexOptions.IgnoreCase)) return true;
        }
        return false;
    }

    static IEnumerable<string> ReadFrontmatterValues(string markdown, string keys)
    {
        var match = Regex.Match(markdown, @"\A\s*---\s*\r?\n(?<yaml>.*?)\r?\n---(?:\r?\n|$)", RegexOptions.Singleline);
        if (!match.Success) return [];
        var values = new List<string>();
        var active = false;
        foreach (var line in match.Groups["yaml"].Value.Split('\n'))
        {
            if (Regex.Match(line, @"^(?<key>[\w-]+)\s*:\s*(?<value>.*)$") is { Success: true } field)
            {
                active = Regex.IsMatch(field.Groups["key"].Value, $@"^(?:{keys})$", RegexOptions.IgnoreCase);
                if (active && field.Groups["value"].Value.Trim() is { Length: > 0 } value) values.AddRange(SplitYamlValues(value));
            }
            else if (active && Regex.Match(line, @"^\s+-\s*(?<value>.+?)\s*$") is { Success: true } item)
                values.AddRange(SplitYamlValues(item.Groups["value"].Value));
            else if (line.Length > 0 && !char.IsWhiteSpace(line[0])) active = false;
        }
        return values;
    }

    static IEnumerable<string> SplitYamlValues(string value) => value.Trim().Trim('[', ']').Split(',').Select(item => item.Trim().Trim('"', '\''));

    static string NormalizeTag(string value)
    {
        var tag = value.Trim().TrimStart('#');
        if (tag.StartsWith("[[") && tag.EndsWith("]]")) tag = tag[2..^2].Split('|')[0];
        var fragment = tag.IndexOf('#'); if (fragment >= 0) tag = tag[..fragment];
        var alias = tag.IndexOf('|'); if (alias >= 0) tag = tag[..alias];
        return tag.Trim();
    }

    static string[] SplitTableRow(string line)
    {
        line = line.Trim();
        if (line.StartsWith('|')) line = line[1..];
        if (line.EndsWith('|')) line = line[..^1];
        return line.Split('|').Select(cell => cell.Trim()).ToArray();
    }

    static bool IsSeparatorRow(string[] cells) => cells.Length > 0 && cells.All(cell => Regex.IsMatch(cell, @"^:?-{3,}:?$"));

    static void AddResolved(string value, HashSet<string> matches, Dictionary<string, string> byPath, Dictionary<string, string[]> byTitle)
    {
        var target = Uri.UnescapeDataString(value.Trim().Trim('`', '"', '\''));
        var pipe = target.IndexOf('|'); if (pipe >= 0) target = target[..pipe];
        var hash = target.IndexOf('#'); if (hash >= 0) target = target[..hash];
        target = target.Trim().Replace('\\', '/');
        if (target.EndsWith(".md", StringComparison.OrdinalIgnoreCase)) target = target[..^3];
        if (byPath.TryGetValue(target.TrimStart('/'), out var path)) { matches.Add(Path.GetFullPath(path)); return; }
        if (byTitle.TryGetValue(Path.GetFileName(target) ?? target, out var titled) && titled.Length == 1) matches.Add(Path.GetFullPath(titled[0]));
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

}
