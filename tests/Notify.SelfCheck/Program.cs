using Notify.Desktop;

var root = Path.Combine(Path.GetTempPath(), "notify-selfcheck-" + Guid.NewGuid().ToString("N"));
var vault = Path.Combine(root, "vault"); var output = Path.Combine(root, "packs");
Directory.CreateDirectory(vault);
try
{
    File.WriteAllText(Path.Combine(vault, "match.md"), "---\ntags:\n  - complete\ntag: math\n---\nIntegral definition.");
    File.WriteAllText(Path.Combine(vault, "wrong-tag.md"), "---\ntags:\n  - complete\ntag: history\n---\nIntegral history.");
    var matches = ResearchPack.Search(vault, "integral", "complete, math");
    if (matches.Count != 1 || matches[0].Title != "match") throw new Exception("Vault text and frontmatter tag filtering failed.");
    var tagTemplates = Path.Combine(root, "tag templates"); Directory.CreateDirectory(tagTemplates);
    File.WriteAllText(Path.Combine(tagTemplates, "math.md"), "template");
    if (!ResearchPack.GetTagOptions(tagTemplates).SequenceEqual(new[] { "math" })) throw new Exception("Tag template filename options failed.");
    var folder = ResearchPack.Create(output, "Calculus", matches);
    var pack = File.ReadAllText(Path.Combine(folder, "Calculus.md"));
    if (!pack.Contains("Integral definition.") || !pack.Contains("match.md")) throw new Exception("Research pack compilation failed.");
    Console.WriteLine("Research search and pack self-check passed.");
}
finally { Directory.Delete(root, true); }
