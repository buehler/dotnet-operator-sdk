using System.Text;

using k8s;

using Spectre.Console;

namespace KubeOps.Cli.Output;

internal class ResultOutput
{
    private readonly IAnsiConsole _console;
    private readonly OutputFormat _defaultFormat;
    private readonly Dictionary<string, (object, OutputFormat)> _files = new();

    public ResultOutput(IAnsiConsole console, OutputFormat defaultFormat)
    {
        _console = console;
        _defaultFormat = defaultFormat;
    }

    public void Add(string filename, object content) => _files.Add(filename, (content, _defaultFormat));

    public void Add(string filename, object content, OutputFormat format) => _files.Add(filename, (content, format));

    public async Task Write(string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);
        foreach (var (filename, content) in _files)
        {
            await using var file = File.Open(
                Path.Join(
                    outputDirectory,
                    filename),
                FileMode.Create);
            await file.WriteAsync(Encoding.UTF8.GetBytes(Serialize(content)));
        }
    }

    public void Write()
    {
        _console.Write(new Rule());
        foreach (var (filename, content) in _files)
        {
            _console.MarkupLineInterpolated($"[bold]File:[/] [underline]{filename}[/]");
            _console.WriteLine(Serialize(content));
            _console.Write(new Rule());
        }
    }

    private static string Serialize((object Object, OutputFormat Format) data) => data.Format switch
    {
        OutputFormat.Yaml => KubernetesYaml.Serialize(data.Object),
        OutputFormat.Json => KubernetesJson.Serialize(data.Object),
        OutputFormat.Plain => data.Object.ToString() ?? string.Empty,
        _ => throw new ArgumentException("Unknown output format."),
    };
}
