using McMaster.Extensions.CommandLineUtils;

namespace KubeOps.Cli.Output;

public class ConsoleOutput
{
    private readonly IConsole _console;

    public ConsoleOutput(IConsole console) => _console = console;

    public void Write(
        string content,
        ConsoleColor foreground = ConsoleColor.White,
        ConsoleColor? background = null)
    {
        if (background != null)
        {
            _console.BackgroundColor = background.Value;
        }

        _console.ForegroundColor = foreground;
        _console.Write(content);
        _console.ResetColor();
    }

    public void WriteLine(
        string content,
        ConsoleColor foreground = ConsoleColor.White,
        ConsoleColor? background = null)
    {
        if (background != null)
        {
            _console.BackgroundColor = background.Value;
        }

        _console.ForegroundColor = foreground;
        _console.WriteLine(content);
        _console.ResetColor();
    }

    public void WriteLine() => _console.WriteLine();
}
