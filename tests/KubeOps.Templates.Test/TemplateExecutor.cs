namespace KubeOps.Templates.Test;

public class TemplateExecutor : DotnetExecutor, IDisposable
{
    private string? _outputPath;

    public async Task ExecuteCSharpTemplate(string template, string? name = "Template")
    {
        _outputPath ??= Path.Join(Path.GetTempPath(), Path.GetRandomFileName());
        await ExecuteDotnetProcess(
            $"""new {template} -lang "C#" -n {name} -o {_outputPath} """);
    }

    public async Task ExecuteFSharpTemplate(string template, string? name = "Template")
    {
        _outputPath ??= Path.Join(Path.GetTempPath(), Path.GetRandomFileName());
        await ExecuteDotnetProcess(
            $"""new {template} -lang "F#" -n {name} -o {_outputPath} """);
    }

    public bool FileExists(params string[] name) =>
        _outputPath != null && File.Exists(Path.Join(_outputPath, Path.Combine(name)));

    public bool FileContains(string content, params string[] name)
    {
        var file = File.ReadAllText(Path.Join(_outputPath, Path.Combine(name)));
        return file.Contains(content);
    }

    public void Dispose()
    {
        if (_outputPath != null)
        {
            Directory.Delete(_outputPath, true);
        }
    }
}