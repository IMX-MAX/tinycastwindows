using System.IO.Compression;
using System.Text.Json;

namespace Tinycast.Windows;

static class BackupService
{
    const int FormatVersion = 1;
    static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    public static void Export(string path, AppCore core)
    {
        using var file = File.Create(path);
        using var archive = new ZipArchive(file, ZipArchiveMode.Create);
        Write(archive, "manifest.json", new BackupManifest
        {
            FormatVersion = FormatVersion,
            CreatedAt = DateTimeOffset.UtcNow
        });
        Write(archive, "data.json", new BackupData
        {
            Hotkey = core.Settings.Hotkey,
            Transparency = core.Settings.Transparency,
            MistralModel = core.Settings.MistralModel,
            MistralBaseUrl = core.Settings.MistralBaseUrl,
            SystemPrompt = core.Settings.SystemPrompt,
            SystemPromptEnabled = core.Settings.SystemPromptEnabled,
            FileSearchRoots = core.Settings.FileSearchRoots,
            QuickActions = core.Settings.QuickActions,
            CalculatorHistory = core.Settings.CalculatorHistory,
            CustomCommands = core.Settings.CustomCommands,
            Quicklinks = core.Quicklinks,
            Snippets = core.Snippets,
            Notes = core.Notes,
            Clipboard = core.ClipboardItems,
            Ranking = core.Settings.Ranking
        });
    }

    public static void Import(string path, AppCore core)
    {
        using var file = File.OpenRead(path);
        using var archive = new ZipArchive(file, ZipArchiveMode.Read);
        var manifest = Read<BackupManifest>(archive, "manifest.json");
        if (manifest.FormatVersion != FormatVersion)
            throw new InvalidDataException("This Tinycast backup uses an unsupported format.");
        var data = Read<BackupData>(archive, "data.json");

        core.Settings.Hotkey = data.Hotkey;
        core.Settings.Transparency = data.Transparency;
        core.Settings.MistralModel = data.MistralModel;
        core.Settings.MistralBaseUrl = data.MistralBaseUrl;
        core.Settings.SystemPrompt = data.SystemPrompt;
        core.Settings.SystemPromptEnabled = data.SystemPromptEnabled;
        core.Settings.FileSearchRoots = data.FileSearchRoots;
        core.Settings.QuickActions = data.QuickActions;
        core.Settings.CalculatorHistory = data.CalculatorHistory;
        core.Settings.CustomCommands = data.CustomCommands;
        core.Settings.Ranking = data.Ranking;
        Replace(core.Quicklinks, data.Quicklinks);
        Merge(core.Snippets, data.Snippets, item => item.Name + "\n" + item.Body);
        Merge(core.Notes, data.Notes, item => item.Title + "\n" + item.Body);
        Merge(core.ClipboardItems, data.Clipboard, item => item.Text);
        core.Persist();
        core.RefreshResults();
    }

    static void Write<T>(ZipArchive archive, string name, T value)
    {
        var entry = archive.CreateEntry(name, CompressionLevel.SmallestSize);
        using var stream = entry.Open();
        JsonSerializer.Serialize(stream, value, Json);
    }

    static T Read<T>(ZipArchive archive, string name) where T : new()
    {
        var entry = archive.GetEntry(name)
            ?? throw new InvalidDataException($"Backup is missing {name}.");
        using var stream = entry.Open();
        return JsonSerializer.Deserialize<T>(stream, Json) ?? new T();
    }

    static void Replace<T>(List<T> target, IEnumerable<T> source)
    {
        target.Clear();
        target.AddRange(source);
    }

    static void Merge<T>(List<T> target, IEnumerable<T> source, Func<T, string> key)
    {
        var seen = target.Select(key).ToHashSet(StringComparer.Ordinal);
        foreach (var item in source)
        {
            if (seen.Add(key(item))) target.Add(item);
        }
    }

    sealed class BackupManifest
    {
        public int FormatVersion { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    sealed class BackupData
    {
        public string Hotkey { get; set; } = "Alt+Space";
        public int Transparency { get; set; }
        public string MistralModel { get; set; } = MistralClient.DefaultModel;
        public string MistralBaseUrl { get; set; } = MistralClient.DefaultBaseUrl;
        public string SystemPrompt { get; set; } = "";
        public bool SystemPromptEnabled { get; set; } = true;
        public List<string> FileSearchRoots { get; set; } = [];
        public List<QuickActionDefinition> QuickActions { get; set; } = [];
        public List<CalculatorHistoryEntry> CalculatorHistory { get; set; } = [];
        public List<CustomCommand> CustomCommands { get; set; } = [];
        public List<Quicklink> Quicklinks { get; set; } = [];
        public List<Snippet> Snippets { get; set; } = [];
        public List<Note> Notes { get; set; } = [];
        public List<ClipboardItem> Clipboard { get; set; } = [];
        public Dictionary<string, int> Ranking { get; set; } = new(StringComparer.Ordinal);
    }
}
