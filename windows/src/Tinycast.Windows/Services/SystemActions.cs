using Microsoft.Win32;

namespace Tinycast.Windows;

static class SystemActionRunner
{
    public static string? Run(string id)
    {
        if (!OperatingSystem.IsWindows())
            return Simulate(id);

        switch (id)
        {
            case "lock-screen":
                NativeMethods.LockWorkStation();
                break;
            case "sleep":
                NativeMethods.SetSuspendState(false, true, true);
                break;
            case "restart":
                Shell("shutdown", "/r /t 0");
                break;
            case "shut-down":
                Shell("shutdown", "/s /t 0");
                break;
            case "log-out":
                NativeMethods.ExitWindowsEx(0, 0);
                break;
            case "play-pause":
                Key(NativeMethods.VkMediaPlayPause);
                break;
            case "next-track":
                Key(NativeMethods.VkMediaNext);
                break;
            case "previous-track":
                Key(NativeMethods.VkMediaPrev);
                break;
            case "toggle-mute":
                Key(NativeMethods.VkVolumeMute);
                break;
            case "volume-up":
                Key(NativeMethods.VkVolumeUp);
                break;
            case "volume-down":
                Key(NativeMethods.VkVolumeDown);
                break;
            case "volume-0":
                SetVolume(0);
                break;
            case "volume-50":
                SetVolume(0.5);
                break;
            case "volume-100":
                SetVolume(1);
                break;
            case "show-desktop":
                NativeMethods.keybd_event(NativeMethods.VkLwin, 0, 0, 0);
                NativeMethods.keybd_event(NativeMethods.VkD, 0, 0, 0);
                NativeMethods.keybd_event(NativeMethods.VkD, 0, NativeMethods.KeyeventfKeyup, 0);
                NativeMethods.keybd_event(NativeMethods.VkLwin, 0, NativeMethods.KeyeventfKeyup, 0);
                break;
            case "open-recycle-bin":
                Shell("explorer.exe", "shell:RecycleBinFolder");
                break;
            case "empty-recycle-bin":
                NativeMethods.SHEmptyRecycleBin(nint.Zero, null,
                    NativeMethods.SherbNoconfirmation | NativeMethods.SherbNoprogessui | NativeMethods.SherbNosound);
                break;
            case "toggle-hidden-files":
                ToggleHiddenFiles();
                break;
            case "task-manager":
                Shell("taskmgr", "");
                break;
            case "settings":
                Shell("ms-settings:", "");
                break;
            case "terminal":
                Shell("wt.exe", "");
                break;
            case "notification-center":
                NativeMethods.keybd_event(NativeMethods.VkLwin, 0, 0, 0);
                NativeMethods.keybd_event(0x4E, 0, 0, 0); // N
                NativeMethods.keybd_event(0x4E, 0, NativeMethods.KeyeventfKeyup, 0);
                NativeMethods.keybd_event(NativeMethods.VkLwin, 0, NativeMethods.KeyeventfKeyup, 0);
                break;
            case "dark-mode":
                ToggleDarkMode();
                break;
            default:
                return "Unknown action";
        }
        return null;
    }

    static string Simulate(string id) => $"Would run {id} on Windows.";

    static void Shell(string file, string args) =>
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(file, args) { UseShellExecute = true });

    static void Key(byte vk)
    {
        NativeMethods.keybd_event(vk, 0, 0, 0);
        NativeMethods.keybd_event(vk, 0, NativeMethods.KeyeventfKeyup, 0);
    }

    static void SetVolume(double fraction)
    {
        // Volume keys are relative; a mute + ups is the portable approximation without COM.
        Key(NativeMethods.VkVolumeMute);
        Key(NativeMethods.VkVolumeMute);
        var steps = (int)Math.Round(50 * fraction);
        for (var i = 0; i < 50; i++) Key(NativeMethods.VkVolumeDown);
        for (var i = 0; i < steps; i++) Key(NativeMethods.VkVolumeUp);
    }

    static void ToggleHiddenFiles()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", true);
        if (key is null) return;
        var current = (int)(key.GetValue("Hidden") ?? 2);
        key.SetValue("Hidden", current == 1 ? 2 : 1, RegistryValueKind.DWord);
    }

    static void ToggleDarkMode()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", true);
        if (key is null) return;
        var current = (int)(key.GetValue("AppsUseLightTheme") ?? 1);
        var next = current == 0 ? 1 : 0;
        key.SetValue("AppsUseLightTheme", next, RegistryValueKind.DWord);
        key.SetValue("SystemUsesLightTheme", next, RegistryValueKind.DWord);
    }
}

static class StartupService
{
    const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string Name = "Tinycast";

    public static void Apply(bool enabled)
    {
        if (!OperatingSystem.IsWindows()) return;
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, true);
        if (key is null) return;
        if (enabled)
        {
            var exe = Environment.ProcessPath ?? "";
            key.SetValue(Name, $"\"{exe}\"");
        }
        else
        {
            key.DeleteValue(Name, false);
        }
    }
}

static class Paster
{
    public static async Task PasteTextAsync(Avalonia.Input.Platform.IClipboard? clipboard, string text)
    {
        if (clipboard is null) return;
        await clipboard.SetTextAsync(text);
        if (!OperatingSystem.IsWindows()) return;
        await Task.Delay(40);
        NativeMethods.keybd_event(NativeMethods.VkControl, 0, 0, 0);
        NativeMethods.keybd_event(NativeMethods.VkV, 0, 0, 0);
        NativeMethods.keybd_event(NativeMethods.VkV, 0, NativeMethods.KeyeventfKeyup, 0);
        NativeMethods.keybd_event(NativeMethods.VkControl, 0, NativeMethods.KeyeventfKeyup, 0);
    }
}
