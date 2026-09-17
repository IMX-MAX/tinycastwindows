namespace Tinycast.Windows;

public readonly record struct Rect(int X, int Y, int Width, int Height)
{
    public int Right => X + Width;
    public int Bottom => Y + Height;
}

public enum WindowCommand
{
    LeftHalf, RightHalf, TopHalf, BottomHalf,
    TopLeft, TopRight, BottomLeft, BottomRight,
    LeftThird, CenterThird, RightThird,
    LeftTwoThirds, RightTwoThirds,
    Maximize, Center, AlmostMaximize,
    FirstFourth, SecondFourth, ThirdFourth, FourthFourth,
    NextDisplay, PreviousDisplay,
    MakeLarger, MakeSmaller, CenterMouse
}

public static class WindowPlacement
{
    public static readonly (WindowCommand Command, string Name, string Glyph)[] Catalog =
    [
        (WindowCommand.LeftHalf, "Left Half", "←"),
        (WindowCommand.RightHalf, "Right Half", "→"),
        (WindowCommand.TopHalf, "Top Half", "↑"),
        (WindowCommand.BottomHalf, "Bottom Half", "↓"),
        (WindowCommand.TopLeft, "Top Left Quarter", "↖"),
        (WindowCommand.TopRight, "Top Right Quarter", "↗"),
        (WindowCommand.BottomLeft, "Bottom Left Quarter", "↙"),
        (WindowCommand.BottomRight, "Bottom Right Quarter", "↘"),
        (WindowCommand.LeftThird, "Left Third", "▎"),
        (WindowCommand.CenterThird, "Center Third", "▮"),
        (WindowCommand.RightThird, "Right Third", "▊"),
        (WindowCommand.LeftTwoThirds, "Left Two Thirds", "▌"),
        (WindowCommand.RightTwoThirds, "Right Two Thirds", "▐"),
        (WindowCommand.FirstFourth, "First Fourth", "1"),
        (WindowCommand.SecondFourth, "Second Fourth", "2"),
        (WindowCommand.ThirdFourth, "Third Fourth", "3"),
        (WindowCommand.FourthFourth, "Fourth Fourth", "4"),
        (WindowCommand.Maximize, "Maximize", "□"),
        (WindowCommand.AlmostMaximize, "Almost Maximize", "▢"),
        (WindowCommand.Center, "Center", "✚"),
        (WindowCommand.MakeLarger, "Make Larger", "+"),
        (WindowCommand.MakeSmaller, "Make Smaller", "−"),
    ];

    public static Rect Apply(WindowCommand command, Rect window, Rect screen)
    {
        var w = screen.Width;
        var h = screen.Height;
        var x = screen.X;
        var y = screen.Y;
        return command switch
        {
            WindowCommand.LeftHalf => new Rect(x, y, w / 2, h),
            WindowCommand.RightHalf => new Rect(x + w / 2, y, w - w / 2, h),
            WindowCommand.TopHalf => new Rect(x, y, w, h / 2),
            WindowCommand.BottomHalf => new Rect(x, y + h / 2, w, h - h / 2),
            WindowCommand.TopLeft => new Rect(x, y, w / 2, h / 2),
            WindowCommand.TopRight => new Rect(x + w / 2, y, w - w / 2, h / 2),
            WindowCommand.BottomLeft => new Rect(x, y + h / 2, w / 2, h - h / 2),
            WindowCommand.BottomRight => new Rect(x + w / 2, y + h / 2, w - w / 2, h - h / 2),
            WindowCommand.LeftThird => Slice(screen, 3, 0, 1),
            WindowCommand.CenterThird => Slice(screen, 3, 1, 1),
            WindowCommand.RightThird => Slice(screen, 3, 2, 1),
            WindowCommand.LeftTwoThirds => Slice(screen, 3, 0, 2),
            WindowCommand.RightTwoThirds => Slice(screen, 3, 1, 2),
            WindowCommand.FirstFourth => Slice(screen, 4, 0, 1),
            WindowCommand.SecondFourth => Slice(screen, 4, 1, 1),
            WindowCommand.ThirdFourth => Slice(screen, 4, 2, 1),
            WindowCommand.FourthFourth => Slice(screen, 4, 3, 1),
            WindowCommand.Maximize => screen,
            WindowCommand.AlmostMaximize => Inset(screen, (int)(w * 0.04), (int)(h * 0.04)),
            WindowCommand.Center => Centered(window, screen),
            WindowCommand.MakeLarger => Scale(window, screen, 1.1),
            WindowCommand.MakeSmaller => Scale(window, screen, 0.9),
            _ => window
        };
    }

    static Rect Slice(Rect screen, int parts, int index, int span)
    {
        var slice = screen.Width / parts;
        var x = screen.X + slice * index;
        var width = index + span == parts ? screen.Right - x : slice * span;
        return new Rect(x, screen.Y, width, screen.Height);
    }

    static Rect Inset(Rect screen, int dx, int dy) =>
        new(screen.X + dx, screen.Y + dy, screen.Width - dx * 2, screen.Height - dy * 2);

    static Rect Centered(Rect window, Rect screen)
    {
        var x = screen.X + Math.Max(0, (screen.Width - window.Width) / 2);
        var y = screen.Y + Math.Max(0, (screen.Height - window.Height) / 2);
        return new Rect(x, y, window.Width, window.Height);
    }

    static Rect Scale(Rect window, Rect screen, double factor)
    {
        var width = Math.Clamp((int)(window.Width * factor), 200, screen.Width);
        var height = Math.Clamp((int)(window.Height * factor), 120, screen.Height);
        var x = Math.Clamp(window.X - (width - window.Width) / 2, screen.X, screen.Right - width);
        var y = Math.Clamp(window.Y - (height - window.Height) / 2, screen.Y, screen.Bottom - height);
        return new Rect(x, y, width, height);
    }
}
