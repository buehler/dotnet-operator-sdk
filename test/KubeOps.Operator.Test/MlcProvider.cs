using System.Reflection;
using System.Runtime.InteropServices;

using KubeOps.Transpiler;

using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis.MSBuild;

namespace KubeOps.Operator.Test;

public class MlcProvider : IAsyncLifetime
{
    private static readonly SemaphoreSlim Semaphore = new(1, 1);

    static MlcProvider()
    {
        MSBuildLocator.RegisterDefaults();
    }

    public MetadataLoadContext Mlc { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var assemblyConfigurationAttribute =
            typeof(MlcProvider).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>();
        var buildConfigurationName = assemblyConfigurationAttribute?.Configuration ?? "Debug";

        try
        {
            await Semaphore.WaitAsync();
            using var workspace = MSBuildWorkspace.Create(new Dictionary<string, string>
            {
                { "Configuration", buildConfigurationName },
            });
            workspace.SkipUnrecognizedProjects = true;
            workspace.LoadMetadataForReferencedProjects = true;
            var project = await workspace.OpenProjectAsync("../../../KubeOps.Operator.Test.csproj");

            Mlc = ContextCreator.Create(Directory
                .GetFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll")
                .Concat(Directory.GetFiles(Path.GetDirectoryName(project.OutputFilePath)!, "*.dll"))
                .Distinct(), coreAssemblyName: typeof(object).Assembly.GetName().Name);
        }
        finally
        {
            Semaphore.Release();
        }
    }

    public Task DisposeAsync()
    {
        Mlc.Dispose();
        return Task.CompletedTask;
    }
}
