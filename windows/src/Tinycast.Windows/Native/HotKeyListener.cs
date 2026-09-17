using System.Runtime.InteropServices;

namespace Tinycast.Windows;

sealed class HotKeyListener : IDisposable
{
    public event Action? Pressed;
    public event Action? ClipboardChanged;
    public event Action? RegistrationFailed;

    const int WmHotkey = 0x0312;
    const int WmClipboardUpdate = 0x031D;
    const int WmDestroy = 0x0002;
    const int HotkeyId = 0x54;
    const int HwndMessage = -3;

    nint _hwnd;
    Thread? _thread;
    uint _threadId;
    bool _disposed;
    WndProcDel? _wndProc;

    public void Start(uint modifiers, uint vk)
    {
        if (!OperatingSystem.IsWindows()) return;
        _thread = new Thread(() => Pump(modifiers, vk))
        {
            IsBackground = true,
            Name = "Tinycast.HotKeys"
        };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();
    }

    void Pump(uint modifiers, uint vk)
    {
        _threadId = GetCurrentThreadId();
        var className = "TinycastHotKeyHidden";
        _wndProc = WndProc;
        var wndClass = new WNDCLASS
        {
            lpfnWndProc = _wndProc,
            hInstance = GetModuleHandle(null),
            lpszClassName = className
        };
        RegisterClass(ref wndClass);
        _hwnd = CreateWindowEx(0, className, "Tinycast", 0, 0, 0, 0, 0, new nint(HwndMessage), nint.Zero, wndClass.hInstance, nint.Zero);
        if (!NativeMethods.RegisterHotKey(
                _hwnd, HotkeyId, modifiers | NativeMethods.ModNorepeat, vk))
            RegistrationFailed?.Invoke();
        NativeMethods.AddClipboardFormatListener(_hwnd);

        MSG msg;
        while (GetMessage(out msg, nint.Zero, 0, 0) > 0)
        {
            TranslateMessage(ref msg);
            DispatchMessage(ref msg);
        }
    }

    nint WndProc(nint hWnd, uint msg, nint wParam, nint lParam)
    {
        if (msg == WmHotkey) Pressed?.Invoke();
        if (msg == WmClipboardUpdate) ClipboardChanged?.Invoke();
        if (msg == WmDestroy) PostQuitMessage(0);
        return DefWindowProc(hWnd, msg, wParam, lParam);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_hwnd != nint.Zero)
        {
            NativeMethods.UnregisterHotKey(_hwnd, HotkeyId);
            NativeMethods.RemoveClipboardFormatListener(_hwnd);
            PostMessage(_hwnd, WmDestroy, nint.Zero, nint.Zero);
        }
        if (_threadId != 0) PostThreadMessage(_threadId, 0x0012, nint.Zero, nint.Zero);
    }

    delegate nint WndProcDel(nint hWnd, uint msg, nint wParam, nint lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct WNDCLASS
    {
        public uint style;
        public WndProcDel lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public nint hInstance;
        public nint hIcon;
        public nint hCursor;
        public nint hbrBackground;
            public string? lpszMenuName;
        public string lpszClassName;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct MSG
    {
        public nint hwnd;
        public uint message;
        public nint wParam;
        public nint lParam;
        public uint time;
        public int ptX;
        public int ptY;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern ushort RegisterClass(ref WNDCLASS lpWndClass);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern nint CreateWindowEx(int dwExStyle, string lpClassName, string lpWindowName, int dwStyle,
        int x, int y, int nWidth, int nHeight, nint hWndParent, nint hMenu, nint hInstance, nint lpParam);
    [DllImport("user32.dll")] static extern nint DefWindowProc(nint hWnd, uint msg, nint wParam, nint lParam);
    [DllImport("user32.dll")] static extern int GetMessage(out MSG lpMsg, nint hWnd, uint wMsgFilterMin, uint wMsgFilterMax);
    [DllImport("user32.dll")] static extern bool TranslateMessage(ref MSG lpMsg);
    [DllImport("user32.dll")] static extern nint DispatchMessage(ref MSG lpMsg);
    [DllImport("user32.dll")] static extern void PostQuitMessage(int nExitCode);
    [DllImport("user32.dll")] static extern bool PostMessage(nint hWnd, uint Msg, nint wParam, nint lParam);
    [DllImport("user32.dll")] static extern bool PostThreadMessage(uint idThread, uint Msg, nint wParam, nint lParam);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] static extern nint GetModuleHandle(string? lpModuleName);
    [DllImport("kernel32.dll")] static extern uint GetCurrentThreadId();
}
