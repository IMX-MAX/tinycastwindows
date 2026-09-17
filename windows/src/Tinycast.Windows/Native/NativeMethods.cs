using System.Runtime.InteropServices;

namespace Tinycast.Windows;

static class NativeMethods
{
    public const int GwlExstyle = -20;
    public const int WsExLayered = 0x80000;
    public const int WsExToolwindow = 0x80;
    public const int WsExNoactivate = 0x08000000;
    public const int WsExTransparent = 0x20;
    public const int DwmwaSystemBackdropType = 38;
    public const int DwmwaUseImmersiveDarkMode = 20;
    public const int DwmwaWindowCornerPreference = 33;
    public const int DwmsbtNone = 1;
    public const int DwmsbtMainwindow = 2; // Mica
    public const int DwmsbtTransientwindow = 3; // Acrylic
    public const int DwmwcpRound = 2;
    public const uint SwpNomove = 0x0002;
    public const uint SwpNosize = 0x0001;
    public const uint SwpShowwindow = 0x0040;
    public const uint SwpNozorder = 0x0004;
    public const int SwRestore = 9;
    public const int SwMinimize = 6;
    public const int SwShow = 5;
    public const int WmHotkey = 0x0312;
    public const uint ModAlt = 0x0001;
    public const uint ModControl = 0x0002;
    public const uint ModShift = 0x0004;
    public const uint ModWin = 0x0008;
    public const uint ModNorepeat = 0x4000;
    public const uint KeyeventfExtendedkey = 0x0001;
    public const uint KeyeventfKeyup = 0x0002;
    public const byte VkSpace = 0x20;
    public const byte VkLwin = 0x5B;
    public const byte VkD = 0x44;
    public const byte VkVolumeMute = 0xAD;
    public const byte VkVolumeDown = 0xAE;
    public const byte VkVolumeUp = 0xAF;
    public const byte VkMediaNext = 0xB0;
    public const byte VkMediaPrev = 0xB1;
    public const byte VkMediaPlayPause = 0xB3;
    public const byte VkControl = 0x11;
    public const byte VkV = 0x56;
    public const uint InputKeyboard = 1;

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left, Top, Right, Bottom;
        public Rect ToRect() => new(Left, Top, Right - Left, Bottom - Top);
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct INPUT
    {
        public uint Type;
        public InputUnion U;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct InputUnion
    {
        [FieldOffset(0)] public KEYBDINPUT Ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct KEYBDINPUT
    {
        public ushort WVk;
        public ushort WScan;
        public uint DwFlags;
        public uint Time;
        public nuint DwExtraInfo;
    }

    public delegate bool EnumWindowsProc(nint hWnd, nint lParam);

    [DllImport("user32.dll")] public static extern bool RegisterHotKey(nint hWnd, int id, uint fsModifiers, uint vk);
    [DllImport("user32.dll")] public static extern bool UnregisterHotKey(nint hWnd, int id);
    [DllImport("user32.dll")] public static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(nint hWnd);
    [DllImport("user32.dll")] public static extern bool ShowWindow(nint hWnd, int nCmdShow);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(nint hWnd, out RECT lpRect);
    [DllImport("user32.dll")] public static extern bool SetWindowPos(nint hWnd, nint hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(nint hWnd);
    [DllImport("user32.dll")] public static extern int GetWindowText(nint hWnd, System.Text.StringBuilder lpString, int nMaxCount);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(nint hWnd, out uint lpdwProcessId);
    [DllImport("user32.dll")] public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, nint lParam);
    [DllImport("user32.dll")] public static extern nint GetWindowLongPtr(nint hWnd, int nIndex);
    [DllImport("user32.dll")] public static extern nint SetWindowLongPtr(nint hWnd, int nIndex, nint dwNewLong);
    [DllImport("user32.dll")] public static extern bool AddClipboardFormatListener(nint hwnd);
    [DllImport("user32.dll")] public static extern bool RemoveClipboardFormatListener(nint hwnd);
    [DllImport("user32.dll")] public static extern uint GetClipboardSequenceNumber();
    [DllImport("user32.dll")] public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);
    [DllImport("user32.dll")] public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, nuint dwExtraInfo);
    [DllImport("user32.dll")] public static extern bool LockWorkStation();
    [DllImport("user32.dll")] public static extern bool ExitWindowsEx(uint uFlags, uint dwReason);
    [DllImport("user32.dll")] public static extern nint MonitorFromWindow(nint hwnd, uint dwFlags);
    [DllImport("user32.dll")] public static extern bool GetMonitorInfo(nint hMonitor, ref MONITORINFO lpmi);
    [DllImport("dwmapi.dll")] public static extern int DwmSetWindowAttribute(nint hwnd, int attr, ref int attrValue, int attrSize);
    [DllImport("gdi32.dll")] public static extern nint CreateRoundRectRgn(
        int left, int top, int right, int bottom, int widthEllipse, int heightEllipse);
    [DllImport("gdi32.dll")] public static extern bool DeleteObject(nint hObject);
    [DllImport("user32.dll")] public static extern int SetWindowRgn(
        nint hWnd, nint hRgn, bool redraw);
    [DllImport("user32.dll")] public static extern uint GetDpiForWindow(nint hWnd);
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    public static extern int SHEmptyRecycleBin(nint hwnd, string? pszRootPath, uint dwFlags);
    [DllImport("powrprof.dll")] public static extern bool SetSuspendState(bool hibernate, bool forceCritical, bool disableWakeEvent);
    [DllImport("ole32.dll")] public static extern int OleGetClipboard(
        out System.Runtime.InteropServices.ComTypes.IDataObject? dataObject);
    [DllImport("ole32.dll")] public static extern int OleSetClipboard(
        System.Runtime.InteropServices.ComTypes.IDataObject? dataObject);
    [DllImport("ole32.dll")] public static extern int OleFlushClipboard();

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    public const uint MonitorDefaultToNearest = 2;
    public const uint SherbNoconfirmation = 0x00000001;
    public const uint SherbNoprogessui = 0x00000002;
    public const uint SherbNosound = 0x00000004;
}
