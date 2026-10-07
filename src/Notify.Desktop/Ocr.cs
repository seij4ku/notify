using System.Diagnostics;
using System.IO;

namespace Notify.Desktop;

static class Ocr
{
    public static async Task<string> RunAsync(byte[] image)
    {
        var python = MainWindow.FindPython() ?? throw new InvalidOperationException("Python not found. Install Python 3.11/3.12 and Pix2Text using the setup instructions.");
        var worker = Path.Combine(AppContext.BaseDirectory, "ocr_worker.py");
        var start = MainWindow.PythonStart(python, worker);
        start.RedirectStandardInput = true; start.RedirectStandardOutput = true; start.RedirectStandardError = true;
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Could not start the local OCR worker.");
        var stderr = process.StandardError.ReadToEndAsync();
        await process.StandardInput.BaseStream.WriteAsync(image); process.StandardInput.Close();
        var output = await process.StandardOutput.ReadToEndAsync(); await process.WaitForExitAsync();
        if (process.ExitCode != 0) throw new InvalidOperationException("Pix2Text failed: " + (await stderr).Trim());
        return output.Trim();
    }
}
