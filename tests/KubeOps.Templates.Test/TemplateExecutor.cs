namespace KubeOps.Templates.Test;

public class TemplateExecutor : DotnetExecutor, IDisposable
{
    private string? _outputPath;

    public void ExecuteCSharpTemplate(string template, string? name = "Template")
    {
        _outputPath ??= Path.Join(Path.GetTempPath(), Path.GetRandomFileName());
        ExecuteDotnetProcess(
            $@"new {template} -lang ""C#"" -n {name} -o {_outputPath} ");
    }

    public void ExecuteFSharpTemplate(string template, string? name = "Template")
    {
        _outputPath ??= Path.Join(Path.GetTempPath(), Path.GetRandomFileName());
        ExecuteDotnetProcess(
            $@"new {template} -lang ""F#"" -n {name} -o {_outputPath} ");
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
