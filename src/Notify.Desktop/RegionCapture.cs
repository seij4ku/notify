using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace Notify.Desktop;

sealed class RegionCapture : Window
{
    System.Windows.Point start;
    System.Windows.Point current;
    bool selecting;
    readonly TaskCompletionSource<byte[]?> done = new();
    RegionCapture()
    {
        WindowStyle = WindowStyle.None; ResizeMode = ResizeMode.NoResize; AllowsTransparency = true; Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(70, 0, 0, 0));
        Left = SystemParameters.VirtualScreenLeft; Top = SystemParameters.VirtualScreenTop;
        Width = SystemParameters.VirtualScreenWidth; Height = SystemParameters.VirtualScreenHeight;
        Topmost = true; Cursor = System.Windows.Input.Cursors.Cross; WindowStartupLocation = WindowStartupLocation.Manual;
        MouseLeftButtonDown += (_, e) => { selecting = true; start = e.GetPosition(this); CaptureMouse(); };
        MouseMove += (_, e) => { if (selecting) { current = e.GetPosition(this); InvalidateVisual(); } };
        MouseLeftButtonUp += async (_, e) =>
        {
            if (!selecting) return;
            current = e.GetPosition(this); selecting = false; ReleaseMouseCapture(); Hide();
            try { await Task.Delay(100); done.TrySetResult(Crop()); }
            catch (Exception ex) { done.TrySetException(ex); }
            finally { Close(); }
        };
        KeyDown += (_, e) => { if (e.Key == Key.Escape) { done.TrySetResult(null); Close(); } };
        Closed += (_, _) => done.TrySetResult(null);
    }
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        if (!selecting) return;
        var rect = new Rect(start, current); dc.DrawRectangle(System.Windows.Media.Brushes.Transparent, new System.Windows.Media.Pen(System.Windows.Media.Brushes.White, 2), rect);
    }
    byte[]? Crop()
    {
        var source = PresentationSource.FromVisual(this);
        var scale = source?.CompositionTarget?.TransformToDevice ?? Matrix.Identity;
        var x = (int)Math.Round(Math.Min(start.X, current.X) * scale.M11); var y = (int)Math.Round(Math.Min(start.Y, current.Y) * scale.M22);
        var w = (int)Math.Round(Math.Abs(start.X - current.X) * scale.M11); var h = (int)Math.Round(Math.Abs(start.Y - current.Y) * scale.M22); if (w < 2 || h < 2) return null;
        var origin = new NativePoint(); ClientToScreen(new WindowInteropHelper(this).Handle, ref origin);
        using var bitmap = new System.Drawing.Bitmap(w, h); using (var g = System.Drawing.Graphics.FromImage(bitmap)) g.CopyFromScreen(origin.X + x, origin.Y + y, 0, 0, new System.Drawing.Size(w, h));
        using var ms = new MemoryStream(); bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png); return ms.ToArray();
    }
    public static Task<byte[]?> SelectAsync() { var window = new RegionCapture(); window.Show(); window.Activate(); return window.done.Task; }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    struct NativePoint { public int X; public int Y; }

    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    static extern bool ClientToScreen(IntPtr window, ref NativePoint point);
}
