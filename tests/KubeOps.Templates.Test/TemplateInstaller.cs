namespace KubeOps.Templates.Test;

public class TemplateInstaller : DotnetExecutor, IDisposable
{
    private const string Up = "..";

    public TemplateInstaller()
    {
        ExecuteDotnetProcess($"new -i {TemplatesPath}");
    }

    private static string TemplatesPath => Path.GetFullPath(
        Path.Join(
            Directory.GetCurrentDirectory(),
            Up,
            Up,
            Up,
            Up,
            Up,
            "src",
            "KubeOps.Templates"));

    public void Dispose()
    {
        ExecuteDotnetProcess($"new -u {TemplatesPath}");
    }
}
