namespace Tinycast.Windows;

public enum EntryKind
{
    App,
    Command,
    SystemAction,
    Quicklink,
    Snippet,
    Note,
    Emoji,
    File,
    Clipboard,
    Window,
    Calculator,
    AiChat,
    Favorite
}

public sealed record PaletteEntry(
    string Id,
    string Title,
    string Subtitle,
    EntryKind Kind,
    string? Path = null,
    string? Glyph = null,
    object? Payload = null,
    string? IconPath = null)
{
    public bool HasIcon => !string.IsNullOrWhiteSpace(IconPath);
}
