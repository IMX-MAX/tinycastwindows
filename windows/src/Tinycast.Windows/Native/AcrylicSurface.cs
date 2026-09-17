using Avalonia.Controls;

namespace Tinycast.Windows;

static class AcrylicSurface
{
    public static void Apply(Window window, bool windowBackdrop = true)
    {
        if (!OperatingSystem.IsWindows()) return;
        var handle = window.TryGetPlatformHandle()?.Handle ?? nint.Zero;
        if (handle == nint.Zero) return;

        var backdrop = windowBackdrop
            ? NativeMethods.DwmsbtTransientwindow
            : NativeMethods.DwmsbtNone;
        NativeMethods.DwmSetWindowAttribute(
            handle, NativeMethods.DwmwaSystemBackdropType, ref backdrop, sizeof(int));
        var dark = 1;
        NativeMethods.DwmSetWindowAttribute(handle, NativeMethods.DwmwaUseImmersiveDarkMode, ref dark, sizeof(int));
        var round = NativeMethods.DwmwcpRound;
        NativeMethods.DwmSetWindowAttribute(handle, NativeMethods.DwmwaWindowCornerPreference, ref round, sizeof(int));

        var ex = NativeMethods.GetWindowLongPtr(handle, NativeMethods.GwlExstyle);
        NativeMethods.SetWindowLongPtr(handle, NativeMethods.GwlExstyle, ex | NativeMethods.WsExToolwindow);
    }

    public static void ClipRounded(Window window, double radius)
    {
        if (!OperatingSystem.IsWindows()) return;
        var handle = window.TryGetPlatformHandle()?.Handle ?? nint.Zero;
        if (handle == nint.Zero) return;

        var scale = NativeMethods.GetDpiForWindow(handle) / 96.0;
        var width = (int)Math.Ceiling(window.ClientSize.Width * scale);
        var height = (int)Math.Ceiling(window.ClientSize.Height * scale);
        var diameter = Math.Max(1, (int)Math.Round(radius * scale * 2));
        var region = NativeMethods.CreateRoundRectRgn(
            0, 0, width + 1, height + 1, diameter, diameter);
        if (region == nint.Zero) return;
        if (NativeMethods.SetWindowRgn(handle, region, true) == 0)
            NativeMethods.DeleteObject(region);
    }
}
