using Notify.Desktop;

var root = Path.Combine(Path.GetTempPath(), "notify-selfcheck-" + Guid.NewGuid().ToString("N"));
var vault = Path.Combine(root, "vault"); var output = Path.Combine(root, "packs");
Directory.CreateDirectory(vault);
try
{
    File.WriteAllText(Path.Combine(vault, "match.md"), "---\ntags:\n  - complete\ntag: math\n---\nIntegral definition.");
    File.WriteAllText(Path.Combine(vault, "wrong-tag.md"), "---\ntags:\n  - complete\ntag: history\n---\nIntegral history.");
    File.WriteAllText(Path.Combine(vault, "BT1101 match.md"), "---\ntag:\n  - '[[BT1101]]'\n---\nStatistics notes.");
    File.WriteAllText(Path.Combine(vault, "Other tag.md"), "---\ntag:\n  - Other\n---\nStatistics notes.");
    var tagTemplates = Path.Combine(root, "tag templates"); Directory.CreateDirectory(tagTemplates);
    File.WriteAllText(Path.Combine(tagTemplates, "math.md"), "| File |\n| --- |\n| [[match]] |\n");
    File.WriteAllText(Path.Combine(tagTemplates, "BT1101.md"), "---\nindex: \"[[Analytics]]\"\ntag:\n  - BT1101\n---\n![[Tag Template Base.base]]\n");
    File.WriteAllText(Path.Combine(vault, "Tag Template Base.base"), "views:\n  - type: table\n    filters:\n      and:\n        - list(tag).contains(this)\n");
    var matches = ResearchPack.Search(vault, "integral", "math", tagTemplates);
    if (matches.Count != 1 || matches[0].Title != "match") throw new Exception("Vault text and tag-template table filtering failed.");
    if (ResearchPack.Search(vault, "integral", "math", tagTemplates).Any(note => note.Title == "wrong-tag")) throw new Exception("Tag template table did not constrain search results.");
    if (!ResearchPack.GetTagOptions(tagTemplates).SequenceEqual(new[] { "BT1101", "math" })) throw new Exception("Tag template filename options failed.");
    var dynamicMatches = ResearchPack.Search(vault, "Statistics", "BT1101", tagTemplates);
    if (dynamicMatches.Count != 1 || dynamicMatches[0].Title != "BT1101 match") throw new Exception("Embedded Obsidian Base tag filtering failed.");
    var folder = ResearchPack.Create(output, "Calculus", matches);
    var pack = File.ReadAllText(Path.Combine(folder, "Calculus.md"));
    if (!pack.Contains("Integral definition.") || !pack.Contains("match.md")) throw new Exception("Research pack compilation failed.");
    Console.WriteLine("Research search and pack self-check passed.");
}
finally { Directory.Delete(root, true); }
