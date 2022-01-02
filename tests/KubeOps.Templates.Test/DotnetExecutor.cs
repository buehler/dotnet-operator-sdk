using System.Diagnostics;

namespace KubeOps.Templates.Test;

public abstract class DotnetExecutor
{
    protected static void ExecuteDotnetProcess(string arguments)
    {
        var process = new Process { StartInfo = new() { FileName = "dotnet", Arguments = arguments, }, };

        process.Start();
        process.WaitForExit(5000);
    }
}
