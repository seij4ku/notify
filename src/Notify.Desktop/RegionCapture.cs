using System.Drawing;
using System.Drawing.Imaging;
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
        Left = System.Windows.Forms.SystemInformation.VirtualScreen.Left; Top = System.Windows.Forms.SystemInformation.VirtualScreen.Top;
        Width = System.Windows.Forms.SystemInformation.VirtualScreen.Width; Height = System.Windows.Forms.SystemInformation.VirtualScreen.Height;
        Topmost = true; Cursor = System.Windows.Input.Cursors.Cross; WindowStartupLocation = WindowStartupLocation.Manual;
        MouseLeftButtonDown += (_, e) => { selecting = true; start = e.GetPosition(this); CaptureMouse(); };
        MouseMove += (_, e) => { if (selecting) { current = e.GetPosition(this); InvalidateVisual(); } };
        MouseLeftButtonUp += (_, _) => { if (!selecting) return; selecting = false; ReleaseMouseCapture(); done.TrySetResult(Crop()); Close(); };
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
        var x = (int)Math.Min(start.X, current.X); var y = (int)Math.Min(start.Y, current.Y);
        var w = (int)Math.Abs(start.X - current.X); var h = (int)Math.Abs(start.Y - current.Y); if (w < 2 || h < 2) return null;
        using var bitmap = new System.Drawing.Bitmap(w, h); using (var g = System.Drawing.Graphics.FromImage(bitmap)) g.CopyFromScreen((int)Left + x, (int)Top + y, 0, 0, new System.Drawing.Size(w, h));
        using var ms = new MemoryStream(); bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png); return ms.ToArray();
    }
    public static Task<byte[]?> SelectAsync() { var window = new RegionCapture(); window.Show(); window.Activate(); return window.done.Task; }
}
