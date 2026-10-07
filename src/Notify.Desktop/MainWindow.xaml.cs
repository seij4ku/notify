using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Notify.Desktop;

public partial class MainWindow : Window
{
    readonly string settings = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Notify", "vault.txt");
    public MainWindow()
    {
        InitializeComponent();
        FolderBox.Text = File.Exists(settings) ? File.ReadAllText(settings) : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "iCloudDrive", "iCloud~md~obsidian", "Obsidian Vault", "3. Rough Notes");
        FolderBox.TextChanged += (_, _) => { Directory.CreateDirectory(Path.GetDirectoryName(settings)!); File.WriteAllText(settings, FolderBox.Text); };
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
            Status.Text = "Image copied. Running local OCR…";
            ResultBox.Text = await Ocr.RunAsync(bytes);
            Status.Text = "OCR complete. Edit the result, then copy it.";
        }
        catch (Exception ex) { Status.Text = ex.Message; }
        finally { Show(); Activate(); }
    }

    void Browse_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog { SelectedPath = FolderBox.Text, Description = "Choose your Obsidian rough-notes folder" };
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) FolderBox.Text = dialog.SelectedPath;
    }
    void Copy_Click(object sender, RoutedEventArgs e) { System.Windows.Clipboard.SetText(ResultBox.Text); Status.Text = "Markdown copied."; }
    async void Install_Click(object sender, RoutedEventArgs e)
    {
        var python = FindPython();
        if (python is null) { Status.Text = "Install Python 3.11 or 3.12, then retry."; return; }
        try
        {
            Status.Text = "Installing Pix2Text…";
            var install = PythonStart(python, "-m", "pip", "install", "pix2text");
            install.RedirectStandardOutput = true; install.RedirectStandardError = true;
            using var p = Process.Start(install) ?? throw new InvalidOperationException("Could not start Python.");
            var stdout = p.StandardOutput.ReadToEndAsync(); var stderr = p.StandardError.ReadToEndAsync(); await p.WaitForExitAsync();
            if (p.ExitCode != 0) throw new InvalidOperationException((await stderr).Trim());
            Status.Text = "Downloading local OCR models…";
            var setup = PythonStart(python, Path.Combine(AppContext.BaseDirectory, "setup_ocr.py"));
            setup.RedirectStandardOutput = true; setup.RedirectStandardError = true;
            using var warm = Process.Start(setup) ?? throw new InvalidOperationException("Could not start model setup.");
            stdout = warm.StandardOutput.ReadToEndAsync(); stderr = warm.StandardError.ReadToEndAsync(); await warm.WaitForExitAsync();
            if (warm.ExitCode != 0) throw new InvalidOperationException((await stderr).Trim());
            Status.Text = "Pix2Text models are ready for offline use.";
        }
        catch (Exception ex) { Status.Text = "OCR setup failed: " + ex.Message; }
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
