using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

namespace Tinycast.Windows;

public partial class App : Application
{
    PaletteWindow? _palette;
    SettingsWindow? _settings;
    HotKeyListener? _hotkeys;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var core = AppCore.Shared;
            core.Start();
            DataContext = new TrayCommands(core);

            _palette = new PaletteWindow(core);
            desktop.MainWindow = _palette;
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            core.PaletteRequested += () => Dispatcher.UIThread.Post(() => _palette.Toggle());
            core.HideRequested += () => Dispatcher.UIThread.Post(() => _palette.Dismiss());
            core.SettingsRequested += () => Dispatcher.UIThread.Post(ShowSettings);
            core.NoteRequested += note => Dispatcher.UIThread.Post(() => new NoteWindow(core, note).Show());
            core.HudRequested += message => Dispatcher.UIThread.Post(() => _palette.ShowHud(message));
            core.ConfirmRequested += id => Dispatcher.UIThread.Post(() => _palette.AskConfirm(id));

            _hotkeys = new HotKeyListener();
            ParseHotkey(core.Settings.Hotkey, out var mods, out var vk);
            _hotkeys.Pressed += () => Dispatcher.UIThread.Post(core.TogglePalette);
            _hotkeys.ClipboardChanged += () => Dispatcher.UIThread.Post(async () =>
            {
                try
                {
                    var text = await _palette.Clipboard!.GetTextAsync();
                    core.CaptureClipboard(text);
                }
                catch { /* clipboard can be locked by another process */ }
            });
            _hotkeys.Start(mods, vk);

            desktop.ShutdownRequested += (_, _) =>
            {
                core.Persist();
                _hotkeys.Dispose();
            };

            if (OperatingSystem.IsWindows())
                Dispatcher.UIThread.Post(() => _palette.Hide());
        }

        base.OnFrameworkInitializationCompleted();
    }

    void ShowSettings()
    {
        if (_settings is { IsVisible: true })
        {
            _settings.Activate();
            return;
        }
        _settings = new SettingsWindow(AppCore.Shared);
        _settings.Show();
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

public sealed class TrayCommands(AppCore core)
{
    public Avalonia.Controls.Window? Owner { get; set; }
    public SimpleCommand TogglePaletteCommand { get; } = new(core.TogglePalette);
    public SimpleCommand OpenSettingsCommand { get; } = new(core.OpenSettings);
    public SimpleCommand QuitCommand { get; } = new(() =>
    {
        core.Persist();
        Environment.Exit(0);
    });
}

public sealed class SimpleCommand(Action action) : System.Windows.Input.ICommand
{
    public event EventHandler? CanExecuteChanged { add { } remove { } }
    public bool CanExecute(object? parameter) => true;
    public void Execute(object? parameter) => action();
}
