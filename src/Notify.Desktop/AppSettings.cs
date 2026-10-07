using System.IO;
using System.Text.Json;

namespace Notify.Desktop;

sealed class AppSettings
{
    public string NotesFolder { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "iCloudDrive", "iCloud~md~obsidian", "Obsidian Vault", "3. Rough Notes");
    public string VaultFolder { get; set; } = Path.GetDirectoryName(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "iCloudDrive", "iCloud~md~obsidian", "Obsidian Vault", "3. Rough Notes"))!;
    public string ResearchFolder { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Notify Research");
    public string TagDirectory { get; set; } = "";
    public string ScreenshotHotkey { get; set; } = "Ctrl+Alt+F12";
}

static class SettingsStore
{
    static readonly string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Notify");
    static readonly string path = Path.Combine(folder, "settings.json");
    static readonly string legacyPath = Path.Combine(folder, "vault.txt");

    public static AppSettings Load()
    {
        var settings = File.Exists(path) ? JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path)) ?? new AppSettings() : new AppSettings();
        if (!File.Exists(path) && File.Exists(legacyPath)) settings.NotesFolder = File.ReadAllText(legacyPath);
        return settings;
    }

    public static void Save(AppSettings settings)
    {
        Directory.CreateDirectory(folder);
        var temp = path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temp, path, true);
        File.WriteAllText(legacyPath, settings.NotesFolder); // Native-host compatibility.
    }
}
