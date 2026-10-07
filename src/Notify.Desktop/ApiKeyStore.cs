using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Notify.Desktop;

static class ApiKeyStore
{
    static readonly string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Notify", "cloud-api-key.bin");

    public static string Read() => File.Exists(path) ? Encoding.UTF8.GetString(Unprotect(File.ReadAllBytes(path))) : "";

    public static void Write(string value)
    {
        if (value.Length == 0) { if (File.Exists(path)) File.Delete(path); return; }
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, Protect(Encoding.UTF8.GetBytes(value)));
    }

    static byte[] Protect(byte[] value) => Transform(value, true);
    static byte[] Unprotect(byte[] value) => Transform(value, false);

    static byte[] Transform(byte[] value, bool protect)
    {
        var pointer = Marshal.AllocHGlobal(value.Length);
        Marshal.Copy(value, 0, pointer, value.Length);
        var input = new DataBlob { Length = value.Length, Data = pointer };
        try
        {
            DataBlob output;
            var ok = protect
                ? CryptProtectData(ref input, "Notify cloud API key", IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, out output)
                : CryptUnprotectData(ref input, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, out output);
            if (!ok) throw new Win32Exception(Marshal.GetLastWin32Error());
            try { var result = new byte[output.Length]; Marshal.Copy(output.Data, result, 0, result.Length); return result; }
            finally { LocalFree(output.Data); }
        }
        finally { Marshal.FreeHGlobal(pointer); }
    }

    [StructLayout(LayoutKind.Sequential)]
    struct DataBlob { public int Length; public IntPtr Data; }

    [DllImport("Crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool CryptProtectData(ref DataBlob input, string description, IntPtr entropy, IntPtr reserved, IntPtr prompt, int flags, out DataBlob output);

    [DllImport("Crypt32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool CryptUnprotectData(ref DataBlob input, IntPtr description, IntPtr entropy, IntPtr reserved, IntPtr prompt, int flags, out DataBlob output);

    [DllImport("Kernel32.dll")]
    static extern IntPtr LocalFree(IntPtr memory);
}
