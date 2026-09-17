using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace Tinycast.Windows;

static class WindowsIconCache
{
    const uint ShgfiIcon = 0x000000100;
    const uint ShgfiLargeicon = 0x000000000;

    public static string? IconPath(string shortcut)
    {
        if (!OperatingSystem.IsWindows() || !File.Exists(shortcut)) return null;
        var cache = Path.Combine(AppPaths.Root, "icon-cache");
        Directory.CreateDirectory(cache);
        var fingerprint = shortcut + "|" + File.GetLastWriteTimeUtc(shortcut).Ticks;
        var name = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(fingerprint)));
        var path = Path.Combine(cache, name + ".png");
        if (File.Exists(path)) return path;

        var info = new SHFILEINFO();
        var result = SHGetFileInfo(
            shortcut, 0, ref info, (uint)Marshal.SizeOf<SHFILEINFO>(),
            ShgfiIcon | ShgfiLargeicon);
        if (result == nint.Zero || info.hIcon == nint.Zero) return null;

        try
        {
            using var shellIcon = Icon.FromHandle(info.hIcon);
            using var icon = (Icon)shellIcon.Clone();
            using var bitmap = icon.ToBitmap();
            bitmap.Save(path, ImageFormat.Png);
            return path;
        }
        catch
        {
            return null;
        }
        finally
        {
            DestroyIcon(info.hIcon);
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct SHFILEINFO
    {
        public nint hIcon;
        public int iIcon;
        public uint dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string? szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string? szTypeName;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    static extern nint SHGetFileInfo(
        string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

    [DllImport("user32.dll")]
    static extern bool DestroyIcon(nint hIcon);
}
