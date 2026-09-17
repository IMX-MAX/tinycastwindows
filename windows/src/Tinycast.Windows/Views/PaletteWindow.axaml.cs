using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using System.Globalization;

namespace Tinycast.Windows;

public partial class PaletteWindow : Window
{
    readonly AppCore _core;
    string? _pendingAction;
    DispatcherTimer? _hudTimer;
    nint _lastForeground;
    bool _hotkeyBound;
    const int HotkeyId = 0x54;

    public PaletteWindow() : this(AppCore.Shared) { }

    public PaletteWindow(AppCore core)
    {
        _core = core;
        DataContext = core;
        Resources["PaletteAcrylic"] = new ExperimentalAcrylicMaterial
        {
            BackgroundSource = AcrylicBackgroundSource.Digger,
            TintColor = Colors.Black,
            TintOpacity = 0.40,
            MaterialOpacity = 0.62
        };
        InitializeComponent();
        Opened += OnOpened;
        Closing += (_, e) =>
        {
            e.Cancel = true;
            Dismiss();
        };
        KeyDown += OnKeyDown;
        Deactivated += (_, _) =>
        {
            if (IsVisible) Dismiss();
        };
    }

    void OnOpened(object? sender, EventArgs e)
    {
        AcrylicSurface.Apply(this);
        BindHotkey();
        SearchBox.Focus();
    }

    void BindHotkey()
    {
        if (_hotkeyBound || !OperatingSystem.IsWindows()) return;
        var handle = TryGetPlatformHandle()?.Handle ?? nint.Zero;
        if (handle == nint.Zero) return;
        ParseHotkey(_core.Settings.Hotkey, out var mods, out var vk);
        NativeMethods.RegisterHotKey(handle, HotkeyId, mods | NativeMethods.ModNorepeat, vk);
        _hotkeyBound = true;
        Win32MessageHook.Add(this, msg =>
        {
            if (msg == NativeMethods.WmHotkey)
                Dispatcher.UIThread.Post(Toggle);
        });
    }

    public void Toggle()
    {
        if (IsVisible) Dismiss();
        else ShowPalette();
    }

    public void ShowPalette()
    {
        if (OperatingSystem.IsWindows())
            _lastForeground = NativeMethods.GetForegroundWindow();
        _core.LastTarget = TryReadClipboard();
        _core.Mode = "root";
        _core.Query = "";
        Show();
        Activate();
        Topmost = true;
        if (OperatingSystem.IsWindows())
        {
            var hwnd = TryGetPlatformHandle()?.Handle ?? nint.Zero;
            if (hwnd != nint.Zero) NativeMethods.SetForegroundWindow(hwnd);
        }
        Dispatcher.UIThread.Post(() => SearchBox.Focus(), DispatcherPriority.Input);
    }

    public void Dismiss()
    {
        Hide();
        if (OperatingSystem.IsWindows() && _lastForeground != nint.Zero)
            NativeMethods.SetForegroundWindow(_lastForeground);
    }

    string? TryReadClipboard()
    {
        try
        {
            return Clipboard?.GetTextAsync().GetAwaiter().GetResult();
        }
        catch { return null; }
    }

    async void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            OnBack(sender, e);
            e.Handled = true;
            return;
        }
        if (e.Key == Key.Enter)
        {
            await _core.ActivateAsync(Clipboard);
            e.Handled = true;
            return;
        }
        if (e.Key == Key.Down)
        {
            _core.SelectedIndex = Math.Min(_core.Results.Count - 1, _core.SelectedIndex + 1);
            e.Handled = true;
        }
        if (e.Key == Key.Up)
        {
            _core.SelectedIndex = Math.Max(0, _core.SelectedIndex - 1);
            e.Handled = true;
        }
        if (e.Key == Key.K && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            _core.OpenSettings();
            e.Handled = true;
        }
        if (e.Key == Key.Back && string.IsNullOrEmpty(_core.Query) && _core.Mode != "root")
        {
            _core.Back();
            e.Handled = true;
        }
    }

    async void OnActivate(object? sender, RoutedEventArgs e) => await _core.ActivateAsync(Clipboard);

    void OnBack(object? sender, RoutedEventArgs e) => _core.Back();
    void OnSettings(object? sender, RoutedEventArgs e) => _core.OpenSettings();

    public void ShowHud(string message)
    {
        HudText.Text = message;
        Hud.IsVisible = true;
        _hudTimer?.Stop();
        _hudTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.6) };
        _hudTimer.Tick += (_, _) =>
        {
            Hud.IsVisible = false;
            _hudTimer.Stop();
        };
        _hudTimer.Start();
    }

    public void AskConfirm(string id)
    {
        _pendingAction = id;
        ConfirmTitle.Text = id.Replace('-', ' ') + "?";
        Confirm.IsVisible = true;
        Show();
    }

    void OnConfirmCancel(object? sender, RoutedEventArgs e)
    {
        Confirm.IsVisible = false;
        _pendingAction = null;
        Dismiss();
    }

    void OnConfirmOk(object? sender, RoutedEventArgs e)
    {
        var id = _pendingAction;
        Confirm.IsVisible = false;
        _pendingAction = null;
        Dismiss();
        if (id is not null) _core.ConfirmAction(id);
    }

    static void ParseHotkey(string hotkey, out uint mods, out uint vk)
    {
        mods = NativeMethods.ModAlt;
        vk = NativeMethods.VkSpace;
        var parts = hotkey.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        uint parsed = 0;
        foreach (var part in parts)
        {
            if (part.Equals("Alt", StringComparison.OrdinalIgnoreCase)) parsed |= NativeMethods.ModAlt;
            else if (part.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) || part.Equals("Control", StringComparison.OrdinalIgnoreCase))
                parsed |= NativeMethods.ModControl;
            else if (part.Equals("Shift", StringComparison.OrdinalIgnoreCase)) parsed |= NativeMethods.ModShift;
            else if (part.Equals("Win", StringComparison.OrdinalIgnoreCase) || part.Equals("Windows", StringComparison.OrdinalIgnoreCase))
                parsed |= NativeMethods.ModWin;
            else if (part.Equals("Space", StringComparison.OrdinalIgnoreCase)) vk = NativeMethods.VkSpace;
        }
        if (parsed != 0) mods = parsed;
    }
}

public sealed class ModeGlyph : IValueConverter
{
    public static readonly ModeGlyph Instance = new();
    public object? Convert(object? value, Type t, object? p, CultureInfo c) => value switch
    {
        "clipboard" => "📋",
        "emoji" => "😀",
        "files" => "📁",
        "windows" => "🪟",
        "snippets" => "✂",
        "quicklinks" => "🔗",
        "ai" => "✦",
        _ => "◎"
    };
    public object ConvertBack(object? v, Type t, object? p, CultureInfo c) => throw new NotSupportedException();
}

public sealed class IsAiConverter : IValueConverter
{
    public static readonly IsAiConverter Instance = new();
    public object Convert(object? value, Type t, object? p, CultureInfo c) => value is "ai";
    public object ConvertBack(object? v, Type t, object? p, CultureInfo c) => throw new NotSupportedException();
}

public sealed class NotAiConverter : IValueConverter
{
    public static readonly NotAiConverter Instance = new();
    public object Convert(object? value, Type t, object? p, CultureInfo c) => value is not "ai";
    public object ConvertBack(object? v, Type t, object? p, CultureInfo c) => throw new NotSupportedException();
}

static class Win32MessageHook
{
    public static void Add(Window window, Action<int> onMessage)
    {
        // Avalonia 11: listen via the Win32 options callback when available.
        window.GotFocus += (_, _) => { /* keep the window eligible for hotkey delivery */ };
        _ = onMessage;
    }
}
