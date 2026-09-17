using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Tinycast.Windows;

public partial class SettingsWindow : Window
{
    readonly AppCore _core;

    public SettingsWindow() : this(AppCore.Shared) { }

    public SettingsWindow(AppCore core)
    {
        _core = core;
        InitializeComponent();
        Opened += (_, _) => AcrylicSurface.Apply(this);
        ShowGeneral(null, null!);
        Closed += (_, _) => _core.Persist();
    }

    void ShowGeneral(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Pane.Children.Clear();
        Pane.Children.Add(Heading("General"));
        Pane.Children.Add(Hint("The palette is a tray app with no taskbar button. Alt+Space summons it. Surfaces use Windows Acrylic — the supported stand-in for macOS Liquid Glass."));
        var login = Check("Launch at login", _core.Settings.LaunchAtLogin, v =>
        {
            _core.Settings.LaunchAtLogin = v;
            StartupService.Apply(v);
        });
        var clip = Check("Clipboard history", _core.Settings.ClipboardEnabled, v => _core.Settings.ClipboardEnabled = v);
        Pane.Children.Add(login);
        Pane.Children.Add(clip);
        Pane.Children.Add(Label("Summon hotkey"));
        var hotkey = new TextBox { Text = _core.Settings.Hotkey, Watermark = "Alt+Space" };
        hotkey.LostFocus += (_, _) =>
        {
            var value = hotkey.Text ?? "";
            if (HotKeyGesture.TryParse(value, out _))
            {
                _core.Settings.Hotkey = value;
                _core.Persist();
            }
            else
            {
                hotkey.Text = _core.Settings.Hotkey;
                _core.ShowNotice("Use a modifier plus Space, a letter, number, or F1–F24");
            }
        };
        Pane.Children.Add(hotkey);
        Pane.Children.Add(Hint("Examples: Alt+Space, Ctrl+Shift+K, Win+F12. Restart Tinycast after changing it."));
    }

    void ShowAiClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => ShowAiPane();

    public void ShowAiPane()
    {
        Pane.Children.Clear();
        Pane.Children.Add(Heading("AI · Mistral"));
        Pane.Children.Add(Hint("On Windows, every AI feature talks to Mistral. Apple Intelligence, ChatGPT subscriptions and other vendor keys are not used. Paste a key from https://console.mistral.ai — it is stored with DPAPI and never written into settings backups."));
        Pane.Children.Add(Check("Enable AI Chat", _core.Settings.AiEnabled, v => _core.Settings.AiEnabled = v));
        Pane.Children.Add(Label("API key"));
        var key = new TextBox
        {
            PasswordChar = '•',
            Watermark = "mistral-…",
            Text = SecretStore.LoadMistralKey() ?? ""
        };
        key.LostFocus += (_, _) =>
        {
            if (!string.IsNullOrWhiteSpace(key.Text)) SecretStore.SaveMistralKey(key.Text);
        };
        Pane.Children.Add(key);
        Pane.Children.Add(Label("Model"));
        var models = new ComboBox { ItemsSource = MistralClient.Models, SelectedItem = _core.Settings.MistralModel };
        models.SelectionChanged += (_, _) =>
        {
            if (models.SelectedItem is string m) _core.Settings.MistralModel = m;
        };
        Pane.Children.Add(models);
        Pane.Children.Add(Label("Base URL"));
        var url = new TextBox { Text = _core.Settings.MistralBaseUrl };
        url.LostFocus += (_, _) => _core.Settings.MistralBaseUrl = url.Text ?? MistralClient.DefaultBaseUrl;
        Pane.Children.Add(url);
        Pane.Children.Add(Check("Send Tinycast preamble + your prompt", _core.Settings.SystemPromptEnabled, v => _core.Settings.SystemPromptEnabled = v));
        Pane.Children.Add(Label("Your instructions"));
        var prompt = new TextBox { Text = _core.Settings.SystemPrompt, AcceptsReturn = true, Height = 120, TextWrapping = TextWrapping.Wrap };
        prompt.LostFocus += (_, _) => _core.Settings.SystemPrompt = prompt.Text ?? "";
        Pane.Children.Add(prompt);
    }

    void ShowQuicklinks(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Pane.Children.Clear();
        Pane.Children.Add(Heading("Quicklinks"));
        Pane.Children.Add(Hint("Targets accept {query}, {clipboard}, {date} and {time}."));
        foreach (var link in _core.Quicklinks.ToList())
            Pane.Children.Add(QuicklinkRow(link));
        var add = new Button { Content = "Add quicklink", Classes = { "Frost" } };
        add.Click += (_, _) =>
        {
            _core.Quicklinks.Add(new Quicklink { Name = "New link", Target = "https://www.google.com/search?q={query}" });
            _core.Persist();
            ShowQuicklinks(null, null!);
        };
        Pane.Children.Add(add);
    }

    Control QuicklinkRow(Quicklink link)
    {
        var name = new TextBox { Text = link.Name, Width = 180 };
        var target = new TextBox { Text = link.Target, HorizontalAlignment = HorizontalAlignment.Stretch };
        name.LostFocus += (_, _) => { link.Name = name.Text ?? ""; _core.Persist(); };
        target.LostFocus += (_, _) => { link.Target = target.Text ?? ""; _core.Persist(); };
        var del = new Button { Content = "Remove", Classes = { "Frost" } };
        del.Click += (_, _) =>
        {
            _core.Quicklinks.Remove(link);
            _core.Persist();
            ShowQuicklinks(null, null!);
        };
        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Children = { name, target, del }
        };
    }

    void ShowSnippets(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Pane.Children.Clear();
        Pane.Children.Add(Heading("Snippets"));
        Pane.Children.Add(Hint("Bodies accept {clipboard}, {date}, {time} and {query}."));
        foreach (var snippet in _core.Snippets.ToList())
            Pane.Children.Add(SnippetRow(snippet));
        var add = new Button { Content = "Add snippet", Classes = { "Frost" } };
        add.Click += (_, _) =>
        {
            _core.Snippets.Add(new Snippet { Name = "New snippet", Body = "" });
            _core.Persist();
            ShowSnippets(null, null!);
        };
        Pane.Children.Add(add);
    }

    Control SnippetRow(Snippet snippet)
    {
        var box = new StackPanel { Spacing = 6 };
        var name = new TextBox { Text = snippet.Name, Watermark = "Name" };
        var keyword = new TextBox { Text = snippet.Keyword, Watermark = "Keyword" };
        var body = new TextBox { Text = snippet.Body, AcceptsReturn = true, Height = 80, TextWrapping = TextWrapping.Wrap };
        name.LostFocus += (_, _) => { snippet.Name = name.Text ?? ""; _core.Persist(); };
        keyword.LostFocus += (_, _) => { snippet.Keyword = keyword.Text ?? ""; _core.Persist(); };
        body.LostFocus += (_, _) => { snippet.Body = body.Text ?? ""; _core.Persist(); };
        var del = new Button { Content = "Remove", Classes = { "Frost" } };
        del.Click += (_, _) =>
        {
            _core.Snippets.Remove(snippet);
            _core.Persist();
            ShowSnippets(null, null!);
        };
        box.Children.Add(name);
        box.Children.Add(keyword);
        box.Children.Add(body);
        box.Children.Add(del);
        return box;
    }

    void ShowFiles(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Pane.Children.Clear();
        Pane.Children.Add(Heading("File Search"));
        Pane.Children.Add(Hint("Windows has no Spotlight. Tinycast walks these folders live — keep the list tight."));
        var roots = new TextBox
        {
            Text = string.Join(Environment.NewLine, _core.Settings.FileSearchRoots),
            AcceptsReturn = true,
            Height = 160,
            TextWrapping = TextWrapping.Wrap
        };
        roots.LostFocus += (_, _) =>
        {
            _core.Settings.FileSearchRoots = (roots.Text ?? "")
                .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
            _core.Persist();
        };
        Pane.Children.Add(roots);
    }

    static TextBlock Heading(string text) => new() { Text = text, FontSize = 24, Margin = new(0, 0, 0, 4) };
    static TextBlock Label(string text) => new() { Text = text, Opacity = 0.7 };
    static TextBlock Hint(string text) => new() { Text = text, TextWrapping = TextWrapping.Wrap, Opacity = 0.55, FontSize = 13 };

    static CheckBox Check(string label, bool value, Action<bool> set)
    {
        var box = new CheckBox { Content = label, IsChecked = value };
        box.IsCheckedChanged += (_, _) => set(box.IsChecked == true);
        return box;
    }
}
