using System.Reflection;
using System.Text;

using k8s.Models;

using KubeOps.Abstractions.Entities.Attributes;
using KubeOps.Abstractions.Rbac;

using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.MSBuild;

namespace KubeOps.Cli.SyntaxObjects;

internal sealed class ProjectParser : IDisposable
{
    private readonly MSBuildWorkspace _workspace = MSBuildWorkspace.Create();
    private readonly Lazy<Task<Assembly>> _assembly;

    static ProjectParser()
    {
        MSBuildLocator.RegisterDefaults();
    }

    public ProjectParser(string projectFile) => _assembly = new Lazy<Task<Assembly>>(AssemblyLoader(projectFile));

    public async IAsyncEnumerable<Type> Entities()
    {
        var assembly = await _assembly.Value;
        foreach (var type in assembly
                     .DefinedTypes
                     .Where(t => t.GetCustomAttributes<KubernetesEntityAttribute>().Any())
                     .Where(type => !type.GetCustomAttributes<IgnoreAttribute>().Any()))
        {
            yield return type;
        }
    }

    public async IAsyncEnumerable<RbacAttribute> RbacAttributes()
    {
        var assembly = await _assembly.Value;

        foreach (var type in assembly
                     .DefinedTypes
                     .SelectMany(t =>
                         t.GetCustomAttributes<GenericRbacAttribute>()))
        {
            yield return type;
        }

        foreach (var type in assembly
                     .DefinedTypes
                     .SelectMany(t =>
                         t.GetCustomAttributes<EntityRbacAttribute>()))
        {
            yield return type;
        }
    }

    public void Dispose() => _workspace.Dispose();

    private Func<Task<Assembly>> AssemblyLoader(string projectFile)
        => async () =>
        {
            var project = await _workspace.OpenProjectAsync(projectFile);
            var compilation = await project.GetCompilationAsync();
            if (compilation is null)
            {
                throw new AggregateException("Compilation could not be found.");
            }

            using var assemblyStream = new MemoryStream();
            switch (compilation.Emit(assemblyStream))
            {
                case { Success: false, Diagnostics: var diag }:
                    throw new AggregateException(
                        $"Compilation failed: {diag.Aggregate(new StringBuilder(), (sb, d) => sb.AppendLine(d.ToString()))}");
            }

            return Assembly.Load(assemblyStream.ToArray());
        };
}
