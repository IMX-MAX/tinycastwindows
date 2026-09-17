namespace Tinycast.Windows;

public readonly record struct HotKeyGesture(uint Modifiers, uint VirtualKey)
{
    public const uint Alt = 0x0001;
    public const uint Control = 0x0002;
    public const uint Shift = 0x0004;
    public const uint Windows = 0x0008;
    public const uint Space = 0x20;

    public static bool TryParse(string raw, out HotKeyGesture gesture)
    {
        gesture = default;
        var parts = raw.Split(
            '+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2) return false;

        uint modifiers = 0;
        uint key = 0;
        foreach (var part in parts)
        {
            if (part.Equals("Alt", StringComparison.OrdinalIgnoreCase)) modifiers |= Alt;
            else if (part.Equals("Ctrl", StringComparison.OrdinalIgnoreCase)
                     || part.Equals("Control", StringComparison.OrdinalIgnoreCase))
                modifiers |= Control;
            else if (part.Equals("Shift", StringComparison.OrdinalIgnoreCase)) modifiers |= Shift;
            else if (part.Equals("Win", StringComparison.OrdinalIgnoreCase)
                     || part.Equals("Windows", StringComparison.OrdinalIgnoreCase))
                modifiers |= Windows;
            else if (part.Equals("Space", StringComparison.OrdinalIgnoreCase)) key = Space;
            else if (part.Length == 1 && char.IsLetterOrDigit(part[0]))
                key = char.ToUpperInvariant(part[0]);
            else if (part.Length is 2 or 3
                     && part[0] is 'F' or 'f'
                     && int.TryParse(part[1..], out var function)
                     && function is >= 1 and <= 24)
                key = (uint)(0x70 + function - 1);
            else
                return false;
        }

        if (modifiers == 0 || key == 0) return false;
        gesture = new HotKeyGesture(modifiers, key);
        return true;
    }
}
