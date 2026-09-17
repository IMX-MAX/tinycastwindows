using System.Diagnostics;

namespace Tinycast.Windows;

public sealed record CommandRunResult(
    string Name,
    string Command,
    string Output,
    string Error,
    int ExitCode,
    TimeSpan Elapsed);

static class CustomCommandRunner
{
    public static async Task<CommandRunResult> RunAsync(
        CustomCommand command,
        string selectedText,
        CancellationToken cancellationToken = default)
    {
        var shell = OperatingSystem.IsWindows() ? "cmd.exe" : "/bin/sh";
        var start = new ProcessStartInfo
        {
            FileName = shell,
            WorkingDirectory = Directory.Exists(command.WorkingDirectory)
                ? command.WorkingDirectory
                : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        if (OperatingSystem.IsWindows())
        {
            start.ArgumentList.Add("/d");
            start.ArgumentList.Add("/s");
            start.ArgumentList.Add("/c");
        }
        else
        {
            start.ArgumentList.Add("-c");
        }
        start.ArgumentList.Add(command.Command);
        start.Environment["TINYCAST"] = "1";
        start.Environment["TINYCAST_SELECTION"] = selectedText;

        using var process = new Process { StartInfo = start };
        var clock = Stopwatch.StartNew();
        process.Start();
        process.StandardInput.Close();
        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        var output = await outputTask;
        var error = await errorTask;
        clock.Stop();
        return new CommandRunResult(
            command.Name, command.Command, Trim(output), Trim(error),
            process.ExitCode, clock.Elapsed);
    }

    static string Trim(string text) =>
        text.Length <= 262_144 ? text : text[^262_144..];
}
