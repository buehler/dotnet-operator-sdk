using System.Text;

namespace KubeOps.Operator.Commands.CommandHelpers;

internal class FileWriter
{
    private readonly TextWriter _outputWriter;

    private readonly IDictionary<string, string> _files = new Dictionary<string, string>();

    public FileWriter(TextWriter outputWriter)
    {
        _outputWriter = outputWriter;
    }

    public void Add(string filename, string content)
    {
        _files.Add(filename, content);
    }

    public async Task OutputAsync(string? outputPath = null)
    {
        if (outputPath == null)
        {
            foreach (var (filename, content) in _files)
            {
                await _outputWriter.WriteLineAsync(filename);
                await _outputWriter.WriteLineAsync(content);
                await _outputWriter.WriteLineAsync();
            }

            return;
        }

        Directory.CreateDirectory(outputPath);
        foreach (var (filename, content) in _files)
        {
            await using var file = File.Open(
                Path.Join(
                    outputPath,
                    filename),
                FileMode.Create);
            await file.WriteAsync(Encoding.UTF8.GetBytes(content));
        }
    }
}
