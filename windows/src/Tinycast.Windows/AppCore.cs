using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Tinycast.Windows;

public sealed class AppCore : INotifyPropertyChanged
{
    public static AppCore Shared { get; } = new();

    readonly JsonStore<AppSettings> _settingsStore = new(AppPaths.Settings);
    readonly JsonStore<List<Quicklink>> _quicklinksStore = new(AppPaths.Quicklinks);
    readonly JsonStore<List<Snippet>> _snippetsStore = new(AppPaths.Snippets);
    readonly JsonStore<List<Note>> _notesStore = new(AppPaths.Notes);
    readonly JsonStore<List<ClipboardItem>> _clipboardStore = new(AppPaths.Clipboard);
    readonly MistralClient _mistral = new();
    readonly FileSearchService _files = new();
    readonly CurrencyRateService _currencyService = new();
    CurrencyRateSnapshot _currencyRates = new();
    CancellationTokenSource? _aiCts;

    public AppSettings Settings { get; private set; } = new();
    internal AppIndex Apps { get; } = new();
    public List<Quicklink> Quicklinks { get; private set; } = [];
    public List<Snippet> Snippets { get; private set; } = [];
    public List<Note> Notes { get; private set; } = [];
    public List<ClipboardItem> ClipboardItems { get; private set; } = [];
    public ObservableCollection<PaletteEntry> Results { get; } = [];
    public ObservableCollection<ChatMessage> Chat { get; } = [];

    public string Query { get => _query; set { if (Set(ref _query, value)) RefreshResults(); } }
    string _query = "";

    public int SelectedIndex { get => _selected; set => Set(ref _selected, Math.Max(0, value)); }
    int _selected;

    public string Mode { get => _mode; set { if (Set(ref _mode, value)) RefreshResults(); } }
    string _mode = "root";

    public string Status { get => _status; set => Set(ref _status, value); }
    string _status = "Tinycast";

    public string AiDraft { get => _aiDraft; set => Set(ref _aiDraft, value); }
    string _aiDraft = "";

    public bool IsStreaming { get => _streaming; set => Set(ref _streaming, value); }
    bool _streaming;

    public string? LastTarget { get; set; }
    public string QuickActionInput { get => _quickActionInput; set => Set(ref _quickActionInput, value); }
    string _quickActionInput = "";
    public string QuickActionOutput { get => _quickActionOutput; set => Set(ref _quickActionOutput, value); }
    string _quickActionOutput = "";
    QuickActionDefinition? _activeQuickAction;
    PaletteEntry? _actionTarget;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action? PaletteRequested;
    public event Action? SettingsRequested;
    public event Action? AiSettingsRequested;
    public event Action? QuickActionSettingsRequested;
    public event Action? BackupSettingsRequested;
    public event Action<Note>? NoteRequested;
    public event Action? HideRequested;
    public event Action<string>? ConfirmRequested;
    public event Action<string>? HudRequested;
    public event Action<CommandRunResult>? CommandOutputRequested;

    public void Start()
    {
        Settings = _settingsStore.Load();
        Quicklinks = _quicklinksStore.Load();
        Snippets = _snippetsStore.Load();
        Notes = _notesStore.Load();
        ClipboardItems = _clipboardStore.Load();
        _currencyRates = _currencyService.LoadCached();
        if (Settings.FileSearchRoots.Count == 0)
        {
            var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var desk = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            if (!string.IsNullOrEmpty(docs)) Settings.FileSearchRoots.Add(docs);
            if (!string.IsNullOrEmpty(desk)) Settings.FileSearchRoots.Add(desk);
        }
        if (Quicklinks.Count == 0)
        {
            Quicklinks.Add(new Quicklink { Name = "Google", Target = "https://www.google.com/search?q={query}" });
            Quicklinks.Add(new Quicklink { Name = "Wikipedia", Target = "https://en.wikipedia.org/wiki/Special:Search?search={query}" });
        }
        Apps.Refresh();
        StartupService.Apply(Settings.LaunchAtLogin);
        RefreshResults();
        _ = RefreshCurrencyRatesAsync();
    }

    public void Persist()
    {
        _settingsStore.Save(Settings);
        _quicklinksStore.Save(Quicklinks);
        _snippetsStore.Save(Snippets);
        _notesStore.Save(Notes);
        _clipboardStore.Save(ClipboardItems.Take(Settings.ClipboardLimit).ToList());
        StartupService.Apply(Settings.LaunchAtLogin);
    }

    public void CaptureClipboard(string? text)
    {
        if (!Settings.ClipboardEnabled || string.IsNullOrWhiteSpace(text)) return;
        if (ClipboardItems.FirstOrDefault()?.Text == text) return;
        ClipboardItems.Insert(0, new ClipboardItem { Text = text });
        if (ClipboardItems.Count > Settings.ClipboardLimit)
            ClipboardItems.RemoveRange(Settings.ClipboardLimit, ClipboardItems.Count - Settings.ClipboardLimit);
        _clipboardStore.Save(ClipboardItems);
    }

    public void TogglePalette() => PaletteRequested?.Invoke();
    public void OpenSettings() => SettingsRequested?.Invoke();
    public void HidePalette() => HideRequested?.Invoke();
    public void OpenNote(Note note) => NoteRequested?.Invoke(note);

    public void RefreshResults()
    {
        Results.Clear();
        IReadOnlyList<PaletteEntry> entries = Mode switch
        {
            "clipboard" => ClipboardEntries(),
            "emoji" => EmojiEntries(),
            "files" => _files.Search(Query, Settings.FileSearchRoots),
            "windows" => WindowEntries(),
            "snippets" => SnippetEntries(),
            "quicklinks" => QuicklinkEntries(),
            "quickActions" => QuickActionEntries(),
            "quickActionResult" => [],
            "calculatorHistory" => CalculatorHistoryEntries(),
            "actions" => ActionEntries(),
            "ai" => [],
            _ => RootEntries()
        };

        foreach (var entry in entries)
            Results.Add(entry);
        SelectedIndex = 0;
        Status = Mode switch
        {
            "ai" => Settings.AiEnabled ? "Mistral" : "AI is off — enable it in Settings",
            "clipboard" => $"{ClipboardItems.Count} items",
            "emoji" => "Emoji",
            "files" => "Files",
            "quickActions" => string.IsNullOrWhiteSpace(LastTarget)
                ? "Select text in another app first"
                : "Selected text",
            "quickActionResult" => _activeQuickAction?.Name ?? "Quick Action",
            "calculatorHistory" => $"{Settings.CalculatorHistory.Count} calculations",
            "actions" => _actionTarget?.Title ?? "Actions",
            _ => Results.Count == 0 ? "No results" : $"{Results.Count} results"
        };
    }

    IReadOnlyList<PaletteEntry> RootEntries()
    {
        var calc = Calculator.Evaluate(Query, _currencyRates.Rates);
        var pool = new List<PaletteEntry>();
        if (calc is { } result && !result.IsError)
        {
            pool.Add(new PaletteEntry(
                "calc", result.Display, result.Expression,
                EntryKind.Calculator, Glyph: "=", Payload: result));
        }
        pool.AddRange(Apps.Apps);
        foreach (var command in BuiltInCommands.All)
            pool.Add(command);
        pool.AddRange(SystemActionCatalog.All.Select(a =>
            new PaletteEntry("sys:" + a.Id, a.Name, "System", EntryKind.SystemAction, Glyph: a.Glyph, Payload: a)));
        pool.AddRange(Quicklinks.Select(ToEntry));
        pool.AddRange(Snippets.Select(ToEntry));
        pool.AddRange(Notes.Select(ToEntry));
        pool.AddRange(Settings.CustomCommands.Where(command => command.Enabled).Select(command =>
            new PaletteEntry(
                "custom:" + command.Id, command.Name, "Custom Command",
                EntryKind.CustomCommand, Glyph: "⌘", Payload: command)));
        foreach (var id in Settings.FavoriteIds)
        {
            var found = pool.FirstOrDefault(e => e.Id == id);
            if (found is not null)
                pool.Add(found with { Kind = EntryKind.Favorite });
        }
        return PaletteSearch.Rank(Query, pool, Settings.Ranking);
    }

    IReadOnlyList<PaletteEntry> ClipboardEntries() =>
        PaletteSearch.Rank(Query, ClipboardItems.Select(c =>
            new PaletteEntry("clip:" + c.Id, Truncate(c.Text, 80), c.CopiedAt.ToLocalTime().ToString("g"), EntryKind.Clipboard, Glyph: "📋", Payload: c.Text)), Settings.Ranking);

    IReadOnlyList<PaletteEntry> EmojiEntries() =>
        PaletteSearch.Rank(Query, EmojiCatalog.All.Select(e =>
            new PaletteEntry("emoji:" + e.Name, $"{e.Glyph}  {e.Name}", "Emoji", EntryKind.Emoji, Glyph: e.Glyph, Payload: e.Glyph)), Settings.Ranking);

    IReadOnlyList<PaletteEntry> WindowEntries()
    {
        var commands = WindowPlacement.Catalog.Select(c =>
            new PaletteEntry("wcmd:" + c.Command, c.Name, "Window", EntryKind.Window, Glyph: c.Glyph, Payload: c.Command));
        var running = WindowManager.RunningWindows();
        return PaletteSearch.Rank(Query, commands.Concat(running), Settings.Ranking);
    }

    IReadOnlyList<PaletteEntry> SnippetEntries() =>
        PaletteSearch.Rank(Query, Snippets.Select(ToEntry), Settings.Ranking);

    IReadOnlyList<PaletteEntry> QuicklinkEntries() =>
        PaletteSearch.Rank(Query, Quicklinks.Select(ToEntry), Settings.Ranking);

    IReadOnlyList<PaletteEntry> QuickActionEntries() =>
        PaletteSearch.Rank(Query, Settings.QuickActions.Select(action =>
            new PaletteEntry(
                "qa:" + action.Id, action.Name, "Mistral Quick Action",
                EntryKind.QuickAction, Glyph: action.Glyph, Payload: action)), Settings.Ranking);

    IReadOnlyList<PaletteEntry> CalculatorHistoryEntries() =>
        PaletteSearch.Rank(Query, Settings.CalculatorHistory.Select((item, index) =>
            new PaletteEntry(
                "calc-history:" + index, item.Display, item.Expression,
                EntryKind.Calculator, Glyph: "=", Payload: new CalcResult(
                    item.Expression, item.Display, item.CopyText, false))), Settings.Ranking);

    IReadOnlyList<PaletteEntry> ActionEntries()
    {
        if (_actionTarget is null) return [];
        var actions = new List<PaletteEntry>
        {
            new("action:open", "Open", _actionTarget.Title, EntryKind.Action, Glyph: "↵")
        };
        if (_actionTarget.Kind is EntryKind.App or EntryKind.Favorite)
        {
            var favorited = Settings.FavoriteIds.Contains(_actionTarget.Id);
            actions.Add(new PaletteEntry(
                "action:favorite", favorited ? "Remove from Favorites" : "Add to Favorites",
                _actionTarget.Title, EntryKind.Action, Glyph: favorited ? "☆" : "★"));
            actions.Add(new PaletteEntry(
                "action:uninstall", "Uninstall Application…",
                "Open Windows Installed Apps", EntryKind.Action, Glyph: "⊘"));
        }
        if (_actionTarget.Kind == EntryKind.Calculator)
        {
            actions.Add(new PaletteEntry(
                "action:calculator-history", "Calculator History",
                "Recent calculations", EntryKind.Action, Glyph: "≡"));
        }
        return actions;
    }

    static PaletteEntry ToEntry(Quicklink q) =>
        new("ql:" + q.Id, q.Name, q.Target, EntryKind.Quicklink, Glyph: "🔗", Payload: q);

    static PaletteEntry ToEntry(Snippet s) =>
        new("snip:" + s.Id, s.Name, string.IsNullOrEmpty(s.Keyword) ? "Snippet" : s.Keyword, EntryKind.Snippet, Glyph: "✂", Payload: s);

    static PaletteEntry ToEntry(Note n) =>
        new("note:" + n.Id, n.Title, "Note", EntryKind.Note, Glyph: "📝", Payload: n);

    public PaletteEntry? Selected =>
        Results.Count == 0 ? null : Results[Math.Clamp(SelectedIndex, 0, Results.Count - 1)];

    public async Task ActivateAsync(Avalonia.Input.Platform.IClipboard? clipboard)
    {
        if (Mode == "ai")
        {
            await SendChatAsync();
            return;
        }
        var entry = Selected;
        if (entry is null) return;
        RankingStore.Record(Settings.Ranking, entry.Id);
        Persist();
        await ActivateEntryAsync(entry, clipboard);
    }

    public async Task ActivateEntryAsync(PaletteEntry entry, Avalonia.Input.Platform.IClipboard? clipboard)
    {
        switch (entry.Kind)
        {
            case EntryKind.App:
            case EntryKind.Favorite when entry.Id.StartsWith("app:"):
                HidePalette();
                AppIndex.Launch(entry);
                break;
            case EntryKind.Command:
            case EntryKind.AiChat:
                await RunCommandAsync(entry.Id, clipboard);
                break;
            case EntryKind.SystemAction:
                HidePalette();
                if (entry.Payload is SystemActionDef { NeedsConfirm: true } action)
                {
                    ConfirmRequested?.Invoke(action.Id);
                    break;
                }
                Notify(SystemActionRunner.Run(entry.Id.StartsWith("sys:") ? entry.Id[4..] : entry.Id));
                break;
            case EntryKind.Quicklink:
                HidePalette();
                OpenQuicklink((Quicklink)entry.Payload!);
                break;
            case EntryKind.Snippet:
                HidePalette();
                await Paster.PasteTextAsync(clipboard, PlaceholderExpander.Expand(((Snippet)entry.Payload!).Body, Query, LastTarget ?? ""));
                break;
            case EntryKind.Note:
                HidePalette();
                OpenNote((Note)entry.Payload!);
                break;
            case EntryKind.Emoji:
                HidePalette();
                await Paster.PasteTextAsync(clipboard, (string)entry.Payload!);
                break;
            case EntryKind.Clipboard:
                HidePalette();
                await Paster.PasteTextAsync(clipboard, (string)entry.Payload!);
                break;
            case EntryKind.File:
                HidePalette();
                OpenPath(entry.Path!);
                break;
            case EntryKind.Calculator:
                var calculation = (CalcResult)entry.Payload!;
                RecordCalculation(calculation);
                HidePalette();
                if (clipboard is not null)
                    await clipboard.SetTextAsync(calculation.CopyText);
                break;
            case EntryKind.Window:
                HidePalette();
                if (entry.Payload is WindowCommand command) WindowManager.Apply(command);
                else if (entry.Payload is nint hwnd) WindowManager.Focus(hwnd);
                break;
            case EntryKind.QuickAction:
                await RunQuickActionAsync((QuickActionDefinition)entry.Payload!);
                break;
            case EntryKind.CustomCommand:
                var custom = (CustomCommand)entry.Payload!;
                if (custom.ConfirmBeforeRunning)
                    ConfirmRequested?.Invoke("custom:" + custom.Id);
                else
                {
                    HidePalette();
                    await RunCustomCommandAsync(custom);
                }
                break;
            case EntryKind.Action:
                await RunEntryActionAsync(entry.Id, clipboard);
                break;
        }
    }

    async Task RunCommandAsync(string id, Avalonia.Input.Platform.IClipboard? clipboard)
    {
        if (!Settings.AiEnabled && id == "cmd:ai")
        {
            HidePalette();
            AiSettingsRequested?.Invoke();
            return;
        }
        if (!Settings.QuickActionsEnabled && id == "cmd:quick-actions")
        {
            HidePalette();
            QuickActionSettingsRequested?.Invoke();
            return;
        }

        switch (id)
        {
            case "cmd:settings":
                HidePalette();
                OpenSettings();
                break;
            case "cmd:clipboard":
                Mode = "clipboard";
                Query = "";
                break;
            case "cmd:emoji":
                Mode = "emoji";
                Query = "";
                break;
            case "cmd:files":
                Mode = "files";
                Query = "";
                break;
            case "cmd:camera":
                HidePalette();
                OpenWindowsUri("ms-camera:");
                break;
            case "cmd:calendar":
                HidePalette();
                OpenWindowsUri("outlookcal:", "https://outlook.live.com/calendar/0/view/month");
                break;
            case "cmd:calculator-history":
                Mode = "calculatorHistory";
                Query = "";
                break;
            case "cmd:windows":
                Mode = "windows";
                Query = "";
                break;
            case "cmd:snippets":
                Mode = "snippets";
                Query = "";
                break;
            case "cmd:quicklinks":
                Mode = "quicklinks";
                Query = "";
                break;
            case "cmd:ai":
                Mode = "ai";
                Query = "";
                Status = Settings.AiEnabled ? "Ask Mistral" : "Enable AI Chat in Settings";
                break;
            case "cmd:quick-actions":
                Mode = "quickActions";
                Query = "";
                break;
            case "cmd:backup":
                HidePalette();
                BackupSettingsRequested?.Invoke();
                break;
            case "cmd:notes":
                HidePalette();
                var note = Notes.FirstOrDefault() ?? NewNote();
                OpenNote(note);
                break;
            case "cmd:quit":
                Persist();
                Environment.Exit(0);
                break;
            default:
                await Task.CompletedTask;
                break;
        }
    }

    async Task RunQuickActionAsync(QuickActionDefinition action)
    {
        var text = LastTarget;
        if (string.IsNullOrWhiteSpace(text))
        {
            Notify("Select text in another app before opening Tinycast");
            return;
        }
        if (!QuickActionPrompt.Admits(text))
        {
            Notify("The selection is larger than Quick Actions' 32 KB limit");
            return;
        }

        _activeQuickAction = action;
        QuickActionInput = text;
        QuickActionOutput = "";
        Mode = "quickActionResult";
        Status = "Mistral is writing…";
        IsStreaming = true;
        _aiCts?.Cancel();
        _aiCts = new CancellationTokenSource();
        try
        {
            var key = SecretStore.LoadMistralKey() ?? "";
            var prompt = QuickActionPrompt.Message(action.Instruction, text);
            var messages = new[]
            {
                new ChatMessage { Role = "user", Content = prompt }
            };
            await foreach (var token in _mistral.StreamChatAsync(
                key,
                Settings.MistralBaseUrl,
                Settings.QuickActionModel,
                messages,
                QuickActionPrompt.SystemInstructions,
                _aiCts.Token))
            {
                QuickActionOutput += token;
            }
            Status = action.Name;
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            QuickActionOutput = ex.Message;
            Status = "Quick Action failed";
        }
        finally
        {
            IsStreaming = false;
        }
    }

    public async Task CopyQuickActionAsync(Avalonia.Input.Platform.IClipboard? clipboard)
    {
        if (clipboard is null || string.IsNullOrWhiteSpace(QuickActionOutput)) return;
        await clipboard.SetTextAsync(QuickActionOutput);
        ShowNotice("Copied result");
    }

    public async Task ReplaceQuickActionAsync(Avalonia.Input.Platform.IClipboard? clipboard)
    {
        if (string.IsNullOrWhiteSpace(QuickActionOutput)) return;
        HidePalette();
        await Paster.PasteTextAsync(clipboard, QuickActionOutput);
    }

    public async Task RetryQuickActionAsync()
    {
        if (_activeQuickAction is not null)
            await RunQuickActionAsync(_activeQuickAction);
    }

    public void ShowActions()
    {
        if (Mode == "actions")
        {
            Back();
            return;
        }
        _actionTarget = Selected;
        if (_actionTarget is null) return;
        Mode = "actions";
        Query = "";
    }

    async Task RunEntryActionAsync(
        string actionId, Avalonia.Input.Platform.IClipboard? clipboard)
    {
        var target = _actionTarget;
        if (target is null) return;
        switch (actionId)
        {
            case "action:open":
                await ActivateEntryAsync(target, clipboard);
                break;
            case "action:favorite":
                if (!Settings.FavoriteIds.Remove(target.Id))
                    Settings.FavoriteIds.Add(target.Id);
                Persist();
                Mode = "root";
                break;
            case "action:uninstall":
                HidePalette();
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(
                    "ms-settings:appsfeatures") { UseShellExecute = true });
                break;
            case "action:calculator-history":
                Mode = "calculatorHistory";
                break;
        }
    }

    public Note NewNote()
    {
        var note = new Note { Title = "Untitled" };
        Notes.Insert(0, note);
        Persist();
        return note;
    }

    public void SaveNote(Note note)
    {
        note.UpdatedAt = DateTimeOffset.UtcNow;
        var index = Notes.FindIndex(n => n.Id == note.Id);
        if (index >= 0) Notes[index] = note;
        else Notes.Insert(0, note);
        Persist();
    }

    void RecordCalculation(CalcResult result)
    {
        Settings.CalculatorHistory.RemoveAll(item =>
            item.Expression == result.Expression && item.CopyText == result.CopyText);
        Settings.CalculatorHistory.Insert(0, new CalculatorHistoryEntry
        {
            Expression = result.Expression,
            Display = result.Display,
            CopyText = result.CopyText
        });
        if (Settings.CalculatorHistory.Count > 100)
            Settings.CalculatorHistory.RemoveRange(
                100, Settings.CalculatorHistory.Count - 100);
        Persist();
    }

    public void ConfirmAction(string id)
    {
        if (id.StartsWith("custom:", StringComparison.Ordinal))
        {
            var command = Settings.CustomCommands.FirstOrDefault(
                item => item.Id == id["custom:".Length..] && item.Enabled);
            if (command is not null) _ = RunCustomCommandAsync(command);
            return;
        }
        Notify(SystemActionRunner.Run(id));
    }

    public string ConfirmationTitle(string id)
    {
        if (id.StartsWith("custom:", StringComparison.Ordinal))
        {
            var command = Settings.CustomCommands.FirstOrDefault(
                item => item.Id == id["custom:".Length..]);
            return command is null ? "Run command?" : $"Run {command.Name}?";
        }
        return id.Replace('-', ' ') + "?";
    }

    async Task RunCustomCommandAsync(CustomCommand command)
    {
        try
        {
            var result = await CustomCommandRunner.RunAsync(command, LastTarget ?? "");
            if (command.ShowOutput || result.ExitCode != 0)
                CommandOutputRequested?.Invoke(result);
            else
                ShowNotice($"{command.Name} finished");
        }
        catch (Exception ex)
        {
            ShowNotice(ex.Message);
        }
    }

    public void ShowNotice(string message)
    {
        Status = message;
        HudRequested?.Invoke(message);
    }

    void OpenQuicklink(Quicklink link)
    {
        var target = PlaceholderExpander.ExpandUrl(link.Target, Query, LastTarget ?? "");
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(target) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Notify(ex.Message);
        }
    }

    static void OpenPath(string path)
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
    }

    static void OpenWindowsUri(string primary, string? fallback = null)
    {
        try
        {
            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo(primary) { UseShellExecute = true });
        }
        catch when (fallback is not null)
        {
            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo(fallback) { UseShellExecute = true });
        }
    }

    public async Task SendChatAsync()
    {
        if (!Settings.AiEnabled)
        {
            Status = "AI Chat is off";
            return;
        }
        var text = string.IsNullOrWhiteSpace(AiDraft) ? Query : AiDraft;
        if (string.IsNullOrWhiteSpace(text)) return;
        Query = "";
        AiDraft = "";
        Chat.Add(new ChatMessage { Role = "user", Content = text });
        var assistant = new ChatMessage { Role = "assistant", Content = "" };
        Chat.Add(assistant);
        IsStreaming = true;
        _aiCts?.Cancel();
        _aiCts = new CancellationTokenSource();
        var key = SecretStore.LoadMistralKey() ?? "";
        var instructions = AiPreamble.Compose(Settings.SystemPromptEnabled, Settings.SystemPrompt);
        try
        {
            var history = Chat.Take(Chat.Count - 1).ToList();
            await foreach (var token in _mistral.StreamChatAsync(
                key, Settings.MistralBaseUrl, Settings.MistralModel, history, instructions, _aiCts.Token))
            {
                assistant.Content += token;
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            assistant.Content = ex.Message;
        }
        finally
        {
            IsStreaming = false;
        }
    }

    public void StopChat() => _aiCts?.Cancel();

    async Task RefreshCurrencyRatesAsync()
    {
        try
        {
            _currencyRates = await _currencyService.RefreshAsync();
            RefreshResults();
        }
        catch
        {
            // The last successful snapshot remains the only offline source.
        }
    }

    public void Back()
    {
        if (Mode != "root")
        {
            Mode = "root";
            Query = "";
        }
        else HidePalette();
    }

    void Notify(string? message)
    {
        if (!string.IsNullOrWhiteSpace(message)) HudRequested?.Invoke(message);
    }

    static string Truncate(string text, int length) =>
        text.Length <= length ? text.Replace('\n', ' ') : text.Replace('\n', ' ')[..length] + "…";

    bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }

    void OnPropertyChanged(string? name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
