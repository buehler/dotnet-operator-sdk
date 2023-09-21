using System.Text;

using k8s;

namespace KubeOps.Cli.Output;

public class ResultOutput
{
    private readonly ConsoleOutput _console;
    private readonly IDictionary<string, object> _files = new Dictionary<string, object>();

    public ResultOutput(ConsoleOutput console) => _console = console;

    public OutputFormat Format { get; set; }

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
        foreach (var (filename, content) in _files)
        {
            _console.WriteLine(filename, ConsoleColor.Cyan);
            _console.WriteLine(Serialize(content));
            _console.WriteLine();
        }
    }

    private string Serialize(object data) => Format switch
    {
        OutputFormat.Yaml => KubernetesYaml.Serialize(data),
        OutputFormat.Json => KubernetesJson.Serialize(data),
        _ => throw new ArgumentException("Unknown output format."),
    };
}
