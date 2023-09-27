using System.Text;

using k8s;

using Spectre.Console;

namespace KubeOps.Cli.Output;

internal class ResultOutput
{
    private readonly IAnsiConsole _console;
    private readonly OutputFormat _format;
    private readonly IDictionary<string, object> _files = new Dictionary<string, object>();

    public ResultOutput(IAnsiConsole console, OutputFormat format)
    {
        _console = console;
        _format = format;
    }

    public void Add(string filename, object content) => _files.Add(filename, content);

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
            _console.MarkupLine($"[bold]File:[/] [underline]{filename}[/]");
            _console.WriteLine(Serialize(content));
            _console.Write(new Rule());
        }
    }

    private string Serialize(object data) => _format switch
    {
        OutputFormat.Yaml => KubernetesYaml.Serialize(data),
        OutputFormat.Json => KubernetesJson.Serialize(data),
        _ => throw new ArgumentException("Unknown output format."),
    };
}
