using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text.Json;

namespace Notify.Desktop;

static class NativeHost
{
    const string PipeName = "NotifyFirefox";
    public static bool HasStandardPipeHandles => IsPipe(GetStdHandle(-10)) && IsPipe(GetStdHandle(-11));

    public static void Run()
    {
        var input = Console.OpenStandardInput(); var output = Console.OpenStandardOutput();
        var header = new byte[4];
        while (ReadFully(input, header))
        {
            var length = BitConverter.ToInt32(header);
            if (length is < 2 or > 8_000_000) return;
            var payload = new byte[length];
            if (!ReadFully(input, payload)) return;
            try
            {
                using var message = JsonDocument.Parse(payload);
                var root = message.RootElement;
                if (!root.TryGetProperty("version", out var version) || version.GetInt32() != 1 ||
                    !root.TryGetProperty("title", out _) || !root.TryGetProperty("html", out _) || !root.TryGetProperty("text", out _) ||
                    !root.TryGetProperty("url", out var url) || !Uri.TryCreate(url.GetString(), UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
                    throw new InvalidDataException("Invalid Firefox capture message.");
                ForwardToDesktop(payload);
                Reply(output, new { ok = true, message = "Capture sent to Notify." });
            }
            catch (Exception ex) { Reply(output, new { ok = false, error = ex.Message }); }
        }
    }

    static void ForwardToDesktop(byte[] payload)
    {
        NamedPipeClientStream? pipe = null;
        for (var attempt = 0; attempt < 8; attempt++)
        {
            try
            {
                pipe = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut, PipeOptions.None);
                pipe.Connect(400);
                break;
            }
            catch (TimeoutException) { pipe?.Dispose(); pipe = null; }
            catch (IOException) { pipe?.Dispose(); pipe = null; }
            if (attempt == 0 && Environment.ProcessPath is { } executable)
                Process.Start(new ProcessStartInfo(executable) { UseShellExecute = true });
            Thread.Sleep(250);
        }
        using (pipe ?? throw new IOException("Could not connect to Notify. Open the desktop app and try again."))
        {
            pipe.Write(BitConverter.GetBytes(payload.Length));
            pipe.Write(payload);
            var header = new byte[4];
            if (!ReadFully(pipe, header)) throw new IOException("Notify did not accept the capture.");
            var length = BitConverter.ToInt32(header);
            if (length is < 2 or > 4096) throw new IOException("Notify returned an invalid response.");
            var response = new byte[length];
            if (!ReadFully(pipe, response)) throw new IOException("Notify closed the capture connection.");
            using var json = JsonDocument.Parse(response);
            if (!json.RootElement.GetProperty("ok").GetBoolean()) throw new IOException(json.RootElement.GetProperty("error").GetString());
        }
    }

    static bool ReadFully(Stream stream, Span<byte> data) { var read = 0; while (read < data.Length) { var n = stream.Read(data[read..]); if (n == 0) return false; read += n; } return true; }
    static void Reply(Stream output, object value) { var bytes = JsonSerializer.SerializeToUtf8Bytes(value); output.Write(BitConverter.GetBytes(bytes.Length)); output.Write(bytes); output.Flush(); }
    static bool IsPipe(IntPtr handle) => handle != IntPtr.Zero && handle != new IntPtr(-1) && GetFileType(handle) == 3;

    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    static extern IntPtr GetStdHandle(int standardHandle);

    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    static extern uint GetFileType(IntPtr handle);
}
