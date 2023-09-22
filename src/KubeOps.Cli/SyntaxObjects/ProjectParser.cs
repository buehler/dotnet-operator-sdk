using System.Reflection;
using System.Text;

using k8s.Models;

using KubeOps.Abstractions.Entities.Attributes;

using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.MSBuild;

namespace KubeOps.Cli.SyntaxObjects;

public sealed class ProjectParser : IDisposable
{
    private readonly MSBuildWorkspace _workspace;
    private readonly Project _project;

    static ProjectParser()
    {
        MSBuildLocator.RegisterDefaults();
    }

    private ProjectParser(MSBuildWorkspace workspace, Project project)
    {
        _workspace = workspace;
        _project = project;
    }

    public static async Task<ProjectParser> CreateAsync(string projectFile)
    {
        var ws = MSBuildWorkspace.Create();
        var project = await ws.OpenProjectAsync(projectFile);
        return new ProjectParser(ws, project);
    }

    public async IAsyncEnumerable<Type> Entities()
    {
        var compilation = await _project.GetCompilationAsync();
        if (compilation is null)
        {
            yield break;
        }

        using var assemblyStream = new MemoryStream();
        switch (compilation.Emit(assemblyStream))
        {
            case { Success: false, Diagnostics: var diag }:
                throw new AggregateException(
                    $"Compilation failed: {diag.Aggregate(new StringBuilder(), (sb, d) => sb.AppendLine(d.ToString()))}");
        }

        var assembly = Assembly.Load(assemblyStream.ToArray());
        foreach (var type in assembly
                     .DefinedTypes
                     .Where(t => t.GetCustomAttributes<KubernetesEntityAttribute>().Any())
                     .Where(type => !type.GetCustomAttributes<IgnoreEntityAttribute>().Any()))
        {
            yield return type;
        }
    }

    public void Dispose() => _workspace.Dispose();
}
