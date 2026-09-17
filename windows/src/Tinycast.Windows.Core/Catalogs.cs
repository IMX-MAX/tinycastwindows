namespace Tinycast.Windows;

public sealed record SystemActionDef(string Id, string Name, string Glyph, bool NeedsConfirm);

public static class SystemActionCatalog
{
    public static readonly IReadOnlyList<SystemActionDef> All =
    [
        new("lock-screen", "Lock Screen", "🔒", false),
        new("sleep", "Sleep", "🌙", false),
        new("restart", "Restart", "↻", true),
        new("shut-down", "Shut Down", "⏻", true),
        new("log-out", "Log Out", "↪", true),
        new("play-pause", "Play / Pause", "⏯", false),
        new("next-track", "Next Track", "⏭", false),
        new("previous-track", "Previous Track", "⏮", false),
        new("toggle-mute", "Toggle Mute", "🔇", false),
        new("volume-up", "Turn Volume Up", "🔊", false),
        new("volume-down", "Turn Volume Down", "🔉", false),
        new("volume-0", "Set Volume to 0%", "🔈", false),
        new("volume-50", "Set Volume to 50%", "🔉", false),
        new("volume-100", "Set Volume to 100%", "🔊", false),
        new("show-desktop", "Show Desktop", "🖥️", false),
        new("open-recycle-bin", "Open Recycle Bin", "🗑️", false),
        new("empty-recycle-bin", "Empty Recycle Bin", "🗑️", true),
        new("toggle-hidden-files", "Toggle Hidden Files", "👁", false),
        new("task-manager", "Open Task Manager", "📊", false),
        new("settings", "Open Windows Settings", "⚙", false),
        new("terminal", "Open Terminal", "⌨", false),
        new("notification-center", "Open Notification Center", "🔔", false),
        new("dark-mode", "Toggle Dark Mode", "◐", false),
    ];
}

public static class BuiltInCommands
{
    public static readonly IReadOnlyList<PaletteEntry> All =
    [
        new("cmd:settings", "Settings", "Open Tinycast settings", EntryKind.Command, Glyph: "⚙"),
        new("cmd:clipboard", "Clipboard History", "Paste from recent copies", EntryKind.Command, Glyph: "📋"),
        new("cmd:emoji", "Emoji", "Search and insert emoji", EntryKind.Command, Glyph: "😀"),
        new("cmd:notes", "Notes", "Floating Markdown notes", EntryKind.Command, Glyph: "📝"),
        new("cmd:files", "Search Files", "Find files in chosen folders", EntryKind.Command, Glyph: "📁"),
        new("cmd:windows", "Window Management", "Snap and move the front window", EntryKind.Command, Glyph: "🪟"),
        new("cmd:snippets", "Snippets", "Expand saved text", EntryKind.Command, Glyph: "✂"),
        new("cmd:quicklinks", "Quicklinks", "Open saved URLs and searches", EntryKind.Command, Glyph: "🔗"),
        new("cmd:ai", "AI Chat", "Chat with Mistral", EntryKind.AiChat, Glyph: "✦"),
        new("cmd:qa-fix", "Fix Grammar", "Rewrite the clipboard with Mistral", EntryKind.AiChat, Glyph: "✦"),
        new("cmd:qa-rewrite", "Rewrite", "Rewrite the clipboard with Mistral", EntryKind.AiChat, Glyph: "✦"),
        new("cmd:qa-summarize", "Summarize", "Summarize the clipboard with Mistral", EntryKind.AiChat, Glyph: "✦"),
        new("cmd:qa-translate", "Translate to English", "Translate the clipboard with Mistral", EntryKind.AiChat, Glyph: "✦"),
        new("cmd:quit", "Quit Tinycast", "Exit the launcher", EntryKind.Command, Glyph: "✕"),
    ];
}

public static class EmojiCatalog
{
    public static readonly IReadOnlyList<(string Glyph, string Name)> All =
    [
        ("😀", "grinning face"), ("😃", "grinning face with big eyes"), ("😄", "grinning face with smiling eyes"),
        ("😁", "beaming face"), ("😆", "grinning squinting face"), ("😅", "grinning face with sweat"),
        ("🤣", "rolling on the floor laughing"), ("😂", "face with tears of joy"), ("🙂", "slightly smiling face"),
        ("😉", "winking face"), ("😊", "smiling face with smiling eyes"), ("😇", "smiling face with halo"),
        ("🥰", "smiling face with hearts"), ("😍", "smiling face with heart-eyes"), ("🤩", "star-struck"),
        ("😘", "face blowing a kiss"), ("😗", "kissing face"), ("😋", "face savoring food"),
        ("😛", "face with tongue"), ("😜", "winking face with tongue"), ("🤪", "zany face"),
        ("🤨", "face with raised eyebrow"), ("🧐", "face with monocle"), ("🤓", "nerd face"),
        ("😎", "smiling face with sunglasses"), ("🥳", "partying face"), ("😏", "smirking face"),
        ("😒", "unamused face"), ("😞", "disappointed face"), ("😔", "pensive face"),
        ("😟", "worried face"), ("😕", "confused face"), ("🙁", "slightly frowning face"),
        ("😣", "persevering face"), ("😖", "confounded face"), ("😫", "tired face"),
        ("🥺", "pleading face"), ("😢", "crying face"), ("😭", "loudly crying face"),
        ("😤", "face with steam"), ("😡", "pouting face"), ("😠", "angry face"),
        ("🤬", "face with symbols"), ("🤯", "exploding head"), ("😳", "flushed face"),
        ("🥵", "hot face"), ("🥶", "cold face"), ("😱", "face screaming in fear"),
        ("😨", "fearful face"), ("🤗", "hugging face"), ("🤔", "thinking face"),
        ("🫡", "saluting face"), ("🤫", "shushing face"), ("🫠", "melting face"),
        ("😴", "sleeping face"), ("🥱", "yawning face"), ("😷", "face with medical mask"),
        ("🤒", "face with thermometer"), ("🤕", "face with head-bandage"), ("🤢", "nauseated face"),
        ("🤮", "face vomiting"), ("🥴", "woozy face"), ("😵", "dizzy face"),
        ("🤠", "cowboy hat face"), ("🥳", "partying face smile"), ("😈", "smiling face with horns"),
        ("👿", "angry face with horns"), ("💀", "skull"), ("👻", "ghost"),
        ("👽", "alien"), ("🤖", "robot"), ("💩", "pile of poo"),
        ("😺", "grinning cat"), ("😸", "grinning cat with smiling eyes"), ("😹", "cat with tears of joy"),
        ("❤️", "red heart"), ("🧡", "orange heart"), ("💛", "yellow heart"),
        ("💚", "green heart"), ("💙", "blue heart"), ("💜", "purple heart"),
        ("🖤", "black heart"), ("🤍", "white heart"), ("💔", "broken heart"),
        ("✨", "sparkles"), ("⭐", "star"), ("🔥", "fire"), ("💯", "hundred points"),
        ("🎉", "party popper"), ("🎊", "confetti ball"), ("🎈", "balloon"),
        ("👍", "thumbs up"), ("👎", "thumbs down"), ("👏", "clapping hands"),
        ("🙌", "raising hands"), ("🤝", "handshake"), ("🙏", "folded hands"),
        ("💪", "flexed biceps"), ("👀", "eyes"), ("🧠", "brain"),
        ("🍕", "pizza"), ("🍔", "hamburger"), ("🍟", "fries"), ("🌮", "taco"),
        ("🍣", "sushi"), ("🍩", "doughnut"), ("☕", "hot beverage"), ("🍺", "beer mug"),
        ("🍷", "wine glass"), ("🍰", "shortcake"), ("🍎", "red apple"),
        ("⚽", "soccer ball"), ("🏀", "basketball"), ("🎮", "video game"),
        ("🎵", "musical note"), ("🎧", "headphone"), ("📷", "camera"),
        ("💻", "laptop"), ("⌨️", "keyboard"), ("📱", "mobile phone"),
        ("💡", "light bulb"), ("🔒", "lock"), ("🔑", "key"),
        ("📌", "pushpin"), ("📎", "paperclip"), ("📁", "file folder"),
        ("📅", "calendar"), ("⏰", "alarm clock"), ("🌙", "crescent moon"),
        ("☀️", "sun"), ("⚡", "high voltage"), ("🌈", "rainbow"),
        ("🌍", "earth africa"), ("🚀", "rocket"), ("✈️", "airplane"),
        ("🚗", "car"), ("🏠", "house"), ("✅", "check mark button"),
        ("❌", "cross mark"), ("⚠️", "warning"), ("❓", "question mark"),
        ("➡️", "right arrow"), ("⬅️", "left arrow"), ("⬆️", "up arrow"), ("⬇️", "down arrow"),
    ];
}
