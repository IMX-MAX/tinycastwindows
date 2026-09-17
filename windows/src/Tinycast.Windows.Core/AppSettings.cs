namespace Tinycast.Windows;

public sealed class AppSettings
{
    public bool LaunchAtLogin { get; set; }
    public string Hotkey { get; set; } = "Alt+Space";
    public int Transparency { get; set; }
    public bool DarkMode { get; set; } = true;
    public bool AiEnabled { get; set; }
    public string MistralModel { get; set; } = "mistral-small-latest";
    public string MistralBaseUrl { get; set; } = "https://api.mistral.ai/v1";
    public string SystemPrompt { get; set; } = "";
    public bool SystemPromptEnabled { get; set; } = true;
    public List<string> FileSearchRoots { get; set; } = [];
    public List<string> FavoriteIds { get; set; } = [];
    public Dictionary<string, int> Ranking { get; set; } = new(StringComparer.Ordinal);
    public bool ClipboardEnabled { get; set; } = true;
    public int ClipboardLimit { get; set; } = 200;
    public List<QuickActionDefinition> QuickActions { get; set; } = QuickActionDefinition.Defaults();
    public List<CalculatorHistoryEntry> CalculatorHistory { get; set; } = [];
    public List<CustomCommand> CustomCommands { get; set; } = [];
}

public sealed class QuickActionDefinition
{
    public string Id { get; set; } = Guid.NewGuid().ToString("n");
    public string Name { get; set; } = "";
    public string Instruction { get; set; } = "";
    public string Glyph { get; set; } = "✦";

    public static List<QuickActionDefinition> Defaults() =>
    [
        new() { Id = "fix", Name = "Fix Grammar", Glyph = "✓", Instruction = "Fix grammar and spelling. Preserve meaning and formatting. Return only the corrected text." },
        new() { Id = "rewrite", Name = "Rewrite Clearly", Glyph = "↻", Instruction = "Rewrite this more clearly and naturally. Preserve meaning. Return only the rewritten text." },
        new() { Id = "shorter", Name = "Make Shorter", Glyph = "−", Instruction = "Make this substantially shorter while preserving the important information. Return only the result." },
        new() { Id = "longer", Name = "Make Longer", Glyph = "+", Instruction = "Expand this with useful detail while preserving its tone and meaning. Return only the result." },
        new() { Id = "tone-professional", Name = "Professional Tone", Glyph = "◇", Instruction = "Rewrite this in a polished professional tone. Return only the result." },
        new() { Id = "tone-friendly", Name = "Friendly Tone", Glyph = "☺", Instruction = "Rewrite this in a warm, friendly tone. Return only the result." },
        new() { Id = "summarize", Name = "Summarize", Glyph = "≡", Instruction = "Summarize this concisely. Return only the summary." },
        new() { Id = "translate", Name = "Translate to English", Glyph = "文", Instruction = "Translate this into natural English. Return only the translation." },
    ];
}

public sealed class CalculatorHistoryEntry
{
    public string Expression { get; set; } = "";
    public string Display { get; set; } = "";
    public string CopyText { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class CustomCommand
{
    public string Id { get; set; } = Guid.NewGuid().ToString("n");
    public string Name { get; set; } = "";
    public string Command { get; set; } = "";
    public bool ConfirmBeforeRunning { get; set; } = true;
    public bool Enabled { get; set; } = true;
    public bool ShowOutput { get; set; }
    public string WorkingDirectory { get; set; } = "";
}

public sealed class Quicklink
{
    public string Id { get; set; } = Guid.NewGuid().ToString("n");
    public string Name { get; set; } = "";
    public string Target { get; set; } = "";
}

public sealed class Snippet
{
    public string Id { get; set; } = Guid.NewGuid().ToString("n");
    public string Name { get; set; } = "";
    public string Keyword { get; set; } = "";
    public string Body { get; set; } = "";
}

public sealed class Note
{
    public string Id { get; set; } = Guid.NewGuid().ToString("n");
    public string Title { get; set; } = "Untitled";
    public string Body { get; set; } = "";
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class ClipboardItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("n");
    public string Text { get; set; } = "";
    public bool IsImage { get; set; }
    public DateTimeOffset CopiedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class ChatMessage : System.ComponentModel.INotifyPropertyChanged
{
    string _content = "";

    public string Role { get; set; } = "user";

    public string Content
    {
        get => _content;
        set
        {
            if (_content == value) return;
            _content = value;
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(Content)));
        }
    }

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
}

public static class PlaceholderExpander
{
    public static string Expand(string template, string query = "", string clipboard = "")
    {
        var now = DateTime.Now;
        return template
            .Replace("{query}", query, StringComparison.OrdinalIgnoreCase)
            .Replace("{clipboard}", clipboard, StringComparison.OrdinalIgnoreCase)
            .Replace("{date}", now.ToString("yyyy-MM-dd"), StringComparison.OrdinalIgnoreCase)
            .Replace("{time}", now.ToString("HH:mm"), StringComparison.OrdinalIgnoreCase)
            .Replace("{datetime}", now.ToString("yyyy-MM-dd HH:mm"), StringComparison.OrdinalIgnoreCase);
    }
}

public static class RankingStore
{
    public static void Record(IDictionary<string, int> ranking, string id)
    {
        ranking.TryGetValue(id, out var count);
        ranking[id] = count + 1;
    }

    public static int Score(IReadOnlyDictionary<string, int> ranking, string id) =>
        ranking.TryGetValue(id, out var count) ? count * 50 : 0;
}

public static class AiPreamble
{
    public const string Text =
        """
        You are a general-purpose assistant. Help with anything the user asks — writing, code, facts, maths, advice or conversation — and never refuse a question for not being about Tinycast.

        You happen to be built into Tinycast for Windows, a system-tray launcher and open-source alternative to PowerToys Run / Raycast. You are reached from its command palette: its search field is your composer, Enter sends a message, and the AI Chat command opens this conversation.

        Tinycast also provides a fuzzy app launcher, global hotkeys, clipboard history, an inline calculator, a floating note, snippets, quicklinks, window management, file search and an emoji picker.

        AI features on Windows talk only to the Mistral API with the user's own key. Compare Tinycast with other tools honestly — you are not here to sell it. Say so when you do not know rather than inventing a feature.
        """;

    public static string? Compose(bool enabled, string userPrompt)
    {
        if (!enabled) return null;
        var trimmed = userPrompt.Trim();
        return trimmed.Length == 0 ? Text : Text + "\n\n" + trimmed;
    }
}
