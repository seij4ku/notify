using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace Notify.Desktop;

static class ScreenshotHotkey
{
    public const int Id = 4701;
    public const int Message = 0x0312;
    const uint ModAlt = 0x0001, ModControl = 0x0002, ModShift = 0x0004, ModWin = 0x0008;

    public static (uint Modifiers, uint Key) Parse(string value)
    {
        var parts = value.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) throw new FormatException("Press a key to use as the screenshot hotkey.");
        uint modifiers = 0;
        foreach (var part in parts[..^1])
            modifiers |= part.ToLowerInvariant() switch { "ctrl" or "control" => ModControl, "alt" => ModAlt, "shift" => ModShift, "win" => ModWin, _ => throw new FormatException($"Unknown hotkey modifier: {part}") };
        if (!Enum.TryParse<Key>(parts[^1], true, out var key) || key == Key.None || key == Key.DeadCharProcessed) throw new FormatException($"Unknown hotkey key: {parts[^1]}");
        if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin) throw new FormatException("Press a non-modifier key.");
        var virtualKey = KeyInterop.VirtualKeyFromKey(key);
        if (virtualKey == 0) throw new FormatException($"Unsupported hotkey key: {parts[^1]}");
        return ((modifiers | 0x4000), (uint)virtualKey);
    }

    public static void Register(IntPtr window, string value)
    {
        var (modifiers, key) = Parse(value);
        if (!RegisterHotKey(window, Id, modifiers, key)) throw new Win32Exception(Marshal.GetLastWin32Error(), "That screenshot hotkey is already in use.");
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool RegisterHotKey(IntPtr window, int id, uint modifiers, uint key);
}
