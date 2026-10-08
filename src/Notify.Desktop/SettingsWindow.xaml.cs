using System.Windows;
using System.Windows.Input;

namespace Notify.Desktop;

public partial class SettingsWindow : Window
{
    internal AppSettings Settings { get; }

    internal SettingsWindow(AppSettings settings)
    {
        InitializeComponent();
        Settings = settings;
        NotesBox.Text = settings.NotesFolder;
        VaultBox.Text = settings.VaultFolder;
        ResearchBox.Text = settings.ResearchFolder;
        TagsDirectoryBox.Text = settings.TagDirectory;
        HotkeyBox.Text = settings.ScreenshotHotkey;
    }

    void BrowseNotes_Click(object sender, RoutedEventArgs e) => Browse(NotesBox);
    void BrowseVault_Click(object sender, RoutedEventArgs e) => Browse(VaultBox);
    void BrowseResearch_Click(object sender, RoutedEventArgs e) => Browse(ResearchBox);
    void BrowseTags_Click(object sender, RoutedEventArgs e) => Browse(TagsDirectoryBox);

    static void Browse(System.Windows.Controls.TextBox box)
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog { SelectedPath = box.Text, Description = "Choose a folder" };
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) box.Text = dialog.SelectedPath;
    }

    void HotkeyBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        e.Handled = true;
        if (e.Key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin) return;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        var modifiers = Keyboard.Modifiers;
        var parts = new List<string>();
        if (modifiers.HasFlag(ModifierKeys.Control)) parts.Add("Ctrl");
        if (modifiers.HasFlag(ModifierKeys.Alt)) parts.Add("Alt");
        if (modifiers.HasFlag(ModifierKeys.Shift)) parts.Add("Shift");
        if (modifiers.HasFlag(ModifierKeys.Windows)) parts.Add("Win");
        parts.Add(key.ToString());
        var value = string.Join("+", parts);
        try { _ = ScreenshotHotkey.Parse(value); HotkeyBox.Text = value; }
        catch (FormatException) { }
    }

    void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _ = ScreenshotHotkey.Parse(HotkeyBox.Text);
            Settings.NotesFolder = NotesBox.Text.Trim();
            Settings.VaultFolder = VaultBox.Text.Trim();
            Settings.ResearchFolder = ResearchBox.Text.Trim();
            Settings.TagDirectory = TagsDirectoryBox.Text.Trim();
            Settings.ScreenshotHotkey = HotkeyBox.Text.Trim();
            DialogResult = true;
        }
        catch (Exception ex) { System.Windows.MessageBox.Show(this, ex.Message, "Could not save settings", MessageBoxButton.OK, MessageBoxImage.Error); }
    }
}
