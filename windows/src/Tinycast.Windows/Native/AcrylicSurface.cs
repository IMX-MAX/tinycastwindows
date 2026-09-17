using Avalonia.Controls;

namespace Tinycast.Windows;

static class AcrylicSurface
{
    /// <summary>
    /// Windows 11 Acrylic (transient backdrop) stands in for macOS Liquid Glass.
    /// Rounded corners + immersive dark mode keep the palette reading as a floating lens.
    /// </summary>
    public static void Apply(Window window)
    {
        if (!OperatingSystem.IsWindows()) return;
        var handle = window.TryGetPlatformHandle()?.Handle ?? nint.Zero;
        if (handle == nint.Zero) return;

        var acrylic = NativeMethods.DwmsbtTransientwindow;
        NativeMethods.DwmSetWindowAttribute(handle, NativeMethods.DwmwaSystemBackdropType, ref acrylic, sizeof(int));
        var dark = 1;
        NativeMethods.DwmSetWindowAttribute(handle, NativeMethods.DwmwaUseImmersiveDarkMode, ref dark, sizeof(int));
        var round = NativeMethods.DwmwcpRound;
        NativeMethods.DwmSetWindowAttribute(handle, NativeMethods.DwmwaWindowCornerPreference, ref round, sizeof(int));

        var ex = NativeMethods.GetWindowLongPtr(handle, NativeMethods.GwlExstyle);
        NativeMethods.SetWindowLongPtr(handle, NativeMethods.GwlExstyle, ex | NativeMethods.WsExToolwindow);
    }
}
