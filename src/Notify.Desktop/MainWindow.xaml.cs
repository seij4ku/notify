using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace Notify.Desktop;

public partial class MainWindow : Window
{
    AppSettings settings = SettingsStore.Load();
    HwndSource? source;
    IntPtr windowHandle;
    bool hotkeyRegistered;
    readonly ObservableCollection<NoteResult> packNotes = [];

    public MainWindow()
    {
        InitializeComponent();
        PackList.ItemsSource = packNotes;
        UpdateSettingsSummary();
        FirefoxStatus.Text = "Not linked yet. Click above to register this app with Firefox.";
        OcrStatus.Text = "Screen OCR uses your saved Claude Code CLI sign-in.";
        PreviewKeyDown += MainWindow_PreviewKeyDown;
        SourceInitialized += (_, _) => RegisterScreenshotHotkey();
        Closed += (_, _) => UnregisterScreenshotHotkey();
    }

    void ShowFirefox_Click(object sender, RoutedEventArgs e) => ShowView(FirefoxView, "Firefox capture");
    void ShowOcr_Click(object sender, RoutedEventArgs e) => ShowView(OcrView, "Screen OCR");
    void ShowResearch_Click(object sender, RoutedEventArgs e) => ShowView(ResearchView, "Research packs");

    void ShowView(UIElement selected, string title)
    {
        FirefoxView.Visibility = Visibility.Collapsed;
        OcrView.Visibility = Visibility.Collapsed;
        ResearchView.Visibility = Visibility.Collapsed;
        selected.Visibility = Visibility.Visible;
        PageTitle.Text = title;
        if (selected == ResearchView) LoadTagOptions();
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
            OcrStatus.Text = "Image copied. Sending a temporary screenshot to Claude Code…";
            ResultBox.Text = await Ocr.RunAsync(bytes);
            OcrStatus.Text = "OCR complete. Edit the result, then copy it.";
        }
        catch (Exception ex) { OcrStatus.Text = ex.Message; }
        finally { Show(); Activate(); }
    }

    void Copy_Click(object sender, RoutedEventArgs e) { System.Windows.Clipboard.SetText(ResultBox.Text); OcrStatus.Text = "Text copied."; }

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
            FirefoxStatus.Text = "Linked. Use the Notify toolbar button to capture a page.";
        }
        catch (Exception ex) { FirefoxStatus.Text = "Firefox link setup failed: " + ex.Message; }
    }

    void Settings_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SettingsWindow(settings) { Owner = this };
        if (dialog.ShowDialog() != true) return;
        try
        {
            SettingsStore.Save(settings);
            UpdateSettingsSummary();
            RegisterScreenshotHotkey();
        }
        catch (Exception ex) { System.Windows.MessageBox.Show(this, ex.Message, "Could not save settings", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    void UpdateSettingsSummary()
    {
        NotesFolderSummary.Text = "Processed notes save to: " + settings.NotesFolder;
        HotkeySummary.Text = "Hotkey: " + settings.ScreenshotHotkey;
    }

    void RegisterScreenshotHotkey()
    {
        if (windowHandle != IntPtr.Zero && hotkeyRegistered) UnregisterHotKey(windowHandle, ScreenshotHotkey.Id);
        windowHandle = new WindowInteropHelper(this).Handle;
        source ??= HwndSource.FromHwnd(windowHandle);
        source?.RemoveHook(WindowProc); source?.AddHook(WindowProc);
        try { ScreenshotHotkey.Register(windowHandle, settings.ScreenshotHotkey); hotkeyRegistered = true; OcrStatus.Text = "Global screenshot hotkey is ready."; }
        catch (Exception ex) { hotkeyRegistered = false; OcrStatus.Text = ex.Message; }
    }

    IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == ScreenshotHotkey.Message && wParam.ToInt32() == ScreenshotHotkey.Id) { handled = true; ShowView(OcrView, "Screen OCR"); Capture_Click(this, new RoutedEventArgs()); }
        return IntPtr.Zero;
    }

    void UnregisterScreenshotHotkey()
    {
        source?.RemoveHook(WindowProc);
        if (windowHandle != IntPtr.Zero && hotkeyRegistered) UnregisterHotKey(windowHandle, ScreenshotHotkey.Id);
    }

    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    static extern bool UnregisterHotKey(IntPtr window, int id);

    async void Search_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ResearchStatus.Text = "Searching vault…";
            var vault = settings.VaultFolder;
            var tagDirectory = settings.TagDirectory;
            var query = QueryBox.Text.Trim();
            var tags = TagsBox.Text.Trim();
            var results = await Task.Run(() => ResearchPack.Search(vault, query, tags, tagDirectory));
            ResultsList.ItemsSource = results;
            ResearchStatus.Text = $"Found {results.Count} matching notes. Add notes to the pack selection before creating it.";
        }
        catch (Exception ex) { ResearchStatus.Text = "Search failed: " + ex.Message; }
    }

    void CreatePack_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var notes = packNotes.ToArray();
            var folder = ResearchPack.Create(settings.ResearchFolder, PackNameBox.Text, notes);
            ResearchStatus.Text = $"Research pack created: {folder}";
        }
        catch (Exception ex) { ResearchStatus.Text = "Could not create pack: " + ex.Message; }
    }

    void AddNotes_Click(object sender, RoutedEventArgs e)
    {
        foreach (var note in ResultsList.SelectedItems.Cast<NoteResult>())
            if (!packNotes.Any(existing => string.Equals(existing.Path, note.Path, StringComparison.OrdinalIgnoreCase))) packNotes.Add(note);
        ResearchStatus.Text = $"{packNotes.Count} notes in the pack selection.";
    }

    void RemoveNotes_Click(object sender, RoutedEventArgs e)
    {
        foreach (var note in PackList.SelectedItems.Cast<NoteResult>().ToArray()) packNotes.Remove(note);
        ResearchStatus.Text = $"{packNotes.Count} notes in the pack selection.";
    }

    void LoadTagOptions()
    {
        try { TagsBox.ItemsSource = ResearchPack.GetTagOptions(settings.TagDirectory); }
        catch (Exception ex) { ResearchStatus.Text = "Could not load tag names: " + ex.Message; }
    }

    void MainWindow_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key != Key.Enter || Keyboard.FocusedElement is System.Windows.Controls.Primitives.TextBoxBase or PasswordBox) return;
        e.Handled = true;
        if (FirefoxView.Visibility == Visibility.Visible) LinkFirefox_Click(this, new RoutedEventArgs());
        else if (OcrView.Visibility == Visibility.Visible) Capture_Click(this, new RoutedEventArgs());
        else if (ResearchView.Visibility == Visibility.Visible) Search_Click(this, new RoutedEventArgs());
    }
}
