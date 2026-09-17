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
}
