using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace Notify.Desktop;

public partial class MainWindow : Window
{
    readonly string settings = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Notify", "vault.txt");
    public MainWindow()
    {
        InitializeComponent();
        FolderBox.Text = File.Exists(settings) ? File.ReadAllText(settings) : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "iCloudDrive", "iCloud~md~obsidian", "Obsidian Vault", "3. Rough Notes");
        FolderBox.TextChanged += (_, _) => { Directory.CreateDirectory(Path.GetDirectoryName(settings)!); File.WriteAllText(settings, FolderBox.Text); };
        FirefoxStatus.Text = "Not linked yet. Click above to register this app with Firefox.";
        OcrStatus.Text = "Region images are copied to the clipboard and processed locally in memory.";
        try { ApiKeyBox.Password = ApiKeyStore.Read(); ApiKeyStatus.Text = ApiKeyBox.Password.Length == 0 ? "No key saved." : "A key is saved for this Windows account."; }
        catch (Exception ex) { ApiKeyStatus.Text = "Could not read saved key: " + ex.Message; }
    }

    async void Capture_Click(object sender, RoutedEventArgs e)
    {
        Hide();
        try
        {
            var bytes = await RegionCapture.SelectAsync();
            if (bytes is null) return;
            using var stream = new MemoryStream(bytes);
            var image = new BitmapImage(); image.BeginInit(); image.CacheOption = BitmapCacheOption.OnLoad; image.StreamSource = stream; image.EndInit();
            System.Windows.Clipboard.SetImage(image);
            OcrStatus.Text = "Image copied. Running local OCR…";
            ResultBox.Text = await Ocr.RunAsync(bytes);
            OcrStatus.Text = "OCR complete. Edit the result, then copy it.";
        }
        catch (Exception ex) { OcrStatus.Text = ex.Message; }
        finally { Show(); Activate(); }
    }

    void Browse_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog { SelectedPath = FolderBox.Text, Description = "Choose your Obsidian rough-notes folder" };
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) FolderBox.Text = dialog.SelectedPath;
    }
    void Copy_Click(object sender, RoutedEventArgs e) { System.Windows.Clipboard.SetText(ResultBox.Text); OcrStatus.Text = "Markdown copied."; }
    void LinkFirefox_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var appFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Notify");
            Directory.CreateDirectory(appFolder);
            var manifest = Path.Combine(appFolder, "native-host.json");
            var executable = Environment.ProcessPath ?? throw new InvalidOperationException("Could not find the running app executable.");
            var json = JsonSerializer.Serialize(new { name = "com.notify.desktop", description = "Notify local page capture", path = executable, type = "stdio", allowed_extensions = new[] { ExtensionIdBox.Text } });
            File.WriteAllText(manifest, json);
            using var key = Registry.CurrentUser.CreateSubKey(@"Software\Mozilla\NativeMessagingHosts\com.notify.desktop") ?? throw new InvalidOperationException("Could not create the Firefox host registry entry.");
            key.SetValue("", manifest, RegistryValueKind.String);
            FirefoxStatus.Text = "Linked. Restart Firefox, then use the Notify toolbar button to capture a page.";
        }
        catch (Exception ex) { FirefoxStatus.Text = "Firefox link setup failed: " + ex.Message; }
    }
    void SaveApiKey_Click(object sender, RoutedEventArgs e)
    {
        try { ApiKeyStore.Write(ApiKeyBox.Password); ApiKeyStatus.Text = ApiKeyBox.Password.Length == 0 ? "Saved key cleared." : "Key saved encrypted for this Windows account."; }
        catch (Exception ex) { ApiKeyStatus.Text = "Could not save key: " + ex.Message; }
    }
    async void Install_Click(object sender, RoutedEventArgs e)
    {
        var python = FindPython();
        if (python is null) { OcrStatus.Text = "Install Python 3.11 or 3.12, then retry."; return; }
        try
        {
            OcrStatus.Text = "Installing Pix2Text…";
            var install = PythonStart(python, "-m", "pip", "install", "pix2text");
            install.RedirectStandardOutput = true; install.RedirectStandardError = true;
            using var p = Process.Start(install) ?? throw new InvalidOperationException("Could not start Python.");
            var stdout = p.StandardOutput.ReadToEndAsync(); var stderr = p.StandardError.ReadToEndAsync(); await p.WaitForExitAsync();
            if (p.ExitCode != 0) throw new InvalidOperationException((await stderr).Trim());
            OcrStatus.Text = "Downloading local OCR models…";
            var setup = PythonStart(python, Path.Combine(AppContext.BaseDirectory, "setup_ocr.py"));
            setup.RedirectStandardOutput = true; setup.RedirectStandardError = true;
            using var warm = Process.Start(setup) ?? throw new InvalidOperationException("Could not start model setup.");
            stdout = warm.StandardOutput.ReadToEndAsync(); stderr = warm.StandardError.ReadToEndAsync(); await warm.WaitForExitAsync();
            if (warm.ExitCode != 0) throw new InvalidOperationException((await stderr).Trim());
            OcrStatus.Text = "Pix2Text models are ready for offline use.";
        }
        catch (Exception ex) { OcrStatus.Text = "OCR setup failed: " + ex.Message; }
    }
    internal static ProcessStartInfo PythonStart(string python, params string[] args)
    {
        var parts = python.Split(' '); var info = new ProcessStartInfo(parts[0]) { UseShellExecute = false, CreateNoWindow = true };
        foreach (var part in parts.Skip(1).Concat(args)) info.ArgumentList.Add(part);
        return info;
    }
    internal static string? FindPython()
    {
        foreach (var candidate in new[] { "py -3.12", "py -3.11", "python" })
        {
            try { using var p = Process.Start(PythonStart(candidate, "--version")); if (p is not null) { p.WaitForExit(3000); if (p.ExitCode == 0) return candidate; } } catch { }
        }
        return null;
    }
}
