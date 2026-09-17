using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Tinycast.Windows;

sealed class AppIndex
{
    readonly List<PaletteEntry> _apps = [];

    public IReadOnlyList<PaletteEntry> Apps => _apps;

    public void Refresh()
    {
        _apps.Clear();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var root in StartMenuRoots())
        {
            if (!Directory.Exists(root)) continue;
            IEnumerable<string> files;
            try
            {
                files = Directory.EnumerateFiles(root, "*.lnk", SearchOption.AllDirectories);
            }
            catch { continue; }

            foreach (var file in files)
            {
                var name = Path.GetFileNameWithoutExtension(file);
                if (string.IsNullOrWhiteSpace(name) || name.Contains("uninstall", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!seen.Add(name)) continue;
                _apps.Add(new PaletteEntry(
                    Id: "app:" + name.ToLowerInvariant(),
                    Title: name,
                    Subtitle: "Application",
                    Kind: EntryKind.App,
                    Path: file,
                    Glyph: "▣",
                    IconPath: WindowsIconCache.IconPath(file)));
            }
        }

        if (_apps.Count == 0)
        {
            foreach (var name in FallbackApps())
            {
                _apps.Add(new PaletteEntry("app:" + name.ToLowerInvariant(), name, "Application", EntryKind.App, Glyph: "▣"));
            }
        }
    }

    static IEnumerable<string> StartMenuRoots()
    {
        yield return Environment.GetFolderPath(Environment.SpecialFolder.StartMenu);
        yield return Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu);
        var programs = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
        if (!string.IsNullOrEmpty(programs)) yield return programs;
    }

    static IEnumerable<string> FallbackApps()
    {
        if (OperatingSystem.IsWindows())
        {
            yield return "Notepad";
            yield return "Calculator";
            yield return "Microsoft Edge";
            yield return "File Explorer";
        }
        else
        {
            yield return "Terminal";
            yield return "Files";
        }
    }

    public static void Launch(PaletteEntry entry)
    {
        if (!string.IsNullOrEmpty(entry.Path) && File.Exists(entry.Path))
        {
            Process.Start(new ProcessStartInfo { FileName = entry.Path, UseShellExecute = true });
            return;
        }

        var name = entry.Title;
        try
        {
            Process.Start(new ProcessStartInfo { FileName = name, UseShellExecute = true });
        }
        catch
        {
            if (OperatingSystem.IsWindows())
                Process.Start(new ProcessStartInfo("explorer.exe", $"shell:AppsFolder") { UseShellExecute = true });
        }
    }
}

sealed class FileSearchService
{
    public IReadOnlyList<PaletteEntry> Search(string query, IEnumerable<string> roots, int limit = 30)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2) return [];
        var q = new FuzzyMatch.Query(query);
        var hits = new List<(PaletteEntry Entry, int Score)>();
        foreach (var root in roots)
        {
            if (!Directory.Exists(root)) continue;
            IEnumerable<string> files;
            try
            {
                files = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories);
            }
            catch { continue; }

            foreach (var file in files.Take(4000))
            {
                var name = Path.GetFileName(file);
                var match = FuzzyMatch.MatchQuery(q, name);
                if (match is null) continue;
                hits.Add((new PaletteEntry("file:" + file, name, file, EntryKind.File, file, "📄"), match.Value.Score));
                if (hits.Count > 200) break;
            }
        }

        return hits.OrderByDescending(h => h.Score).Take(limit).Select(h => h.Entry).ToList();
    }
}

static class WindowManager
{
    public static void Apply(WindowCommand command)
    {
        if (!OperatingSystem.IsWindows()) return;
        var hwnd = NativeMethods.GetForegroundWindow();
        if (hwnd == nint.Zero) return;
        NativeMethods.ShowWindow(hwnd, NativeMethods.SwRestore);
        NativeMethods.GetWindowRect(hwnd, out var rect);
        var window = rect.ToRect();
        var monitor = NativeMethods.MonitorFromWindow(hwnd, NativeMethods.MonitorDefaultToNearest);
        var info = new NativeMethods.MONITORINFO { cbSize = Marshal.SizeOf<NativeMethods.MONITORINFO>() };
        NativeMethods.GetMonitorInfo(monitor, ref info);
        var screen = info.rcWork.ToRect();
        var next = WindowPlacement.Apply(command, window, screen);
        NativeMethods.SetWindowPos(hwnd, nint.Zero, next.X, next.Y, next.Width, next.Height,
            NativeMethods.SwpNozorder | NativeMethods.SwpShowwindow);
    }

    public static IReadOnlyList<PaletteEntry> RunningWindows()
    {
        if (!OperatingSystem.IsWindows()) return [];
        var list = new List<PaletteEntry>();
        NativeMethods.EnumWindows((hWnd, _) =>
        {
            if (!NativeMethods.IsWindowVisible(hWnd)) return true;
            var sb = new System.Text.StringBuilder(512);
            NativeMethods.GetWindowText(hWnd, sb, sb.Capacity);
            var title = sb.ToString();
            if (string.IsNullOrWhiteSpace(title)) return true;
            list.Add(new PaletteEntry("win:" + hWnd, title, "Window", EntryKind.Window, Glyph: "🪟", Payload: hWnd));
            return true;
        }, nint.Zero);
        return list;
    }

    public static void Focus(nint hwnd)
    {
        NativeMethods.ShowWindow(hwnd, NativeMethods.SwRestore);
        NativeMethods.SetForegroundWindow(hwnd);
    }
}
