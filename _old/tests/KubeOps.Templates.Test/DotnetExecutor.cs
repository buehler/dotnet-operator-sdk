using System.Diagnostics;

namespace KubeOps.Templates.Test;

public abstract class DotnetExecutor
{
    protected static async Task ExecuteDotnetProcess(string arguments)
    {
        var process = new Process { StartInfo = new() { FileName = "dotnet", Arguments = arguments, }, };

        process.Start();
        await process.WaitForExitAsync();
    }
}