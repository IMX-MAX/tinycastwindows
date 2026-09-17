using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Tinycast.Windows;

public partial class CommandOutputWindow : Window
{
    string _output = "";

    public CommandOutputWindow()
    {
        InitializeComponent();
    }

    public CommandOutputWindow(CommandRunResult result) : this()
    {
        Title = result.Name;
        NameText.Text = result.Name;
        CommandText.Text = result.Command;
        _output = string.IsNullOrEmpty(result.Output)
            ? result.Error
            : string.IsNullOrEmpty(result.Error)
                ? result.Output
                : result.Output + Environment.NewLine + result.Error;
        OutputText.Text = _output;
        StatusText.Text = result.ExitCode == 0
            ? $"Finished in {result.Elapsed.TotalSeconds:0.0}s"
            : $"Exited {result.ExitCode} after {result.Elapsed.TotalSeconds:0.0}s";
        Opened += (_, _) => AcrylicSurface.Apply(this);
    }

    async void CopyOutput(object? sender, RoutedEventArgs e)
    {
        if (Clipboard is not null) await Clipboard.SetTextAsync(_output);
    }
}
