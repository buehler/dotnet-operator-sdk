using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

using k8s.Models;

using KubeOps.Abstractions.Entities.Attributes;
using KubeOps.Abstractions.Rbac;

using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis.MSBuild;

using Spectre.Console;

namespace KubeOps.Cli.Roslyn;

/// <summary>
/// AssemblyParser.
/// </summary>
internal sealed partial class AssemblyParser
{
    private readonly Assembly[] _assemblies;

    static AssemblyParser()
    {
        MSBuildLocator.RegisterDefaults();
    }

    private AssemblyParser(params Assembly[] assemblies) => _assemblies = assemblies;

    public static Task<AssemblyParser> ForProject(
        IAnsiConsole console,
        FileInfo projectFile)
        => console.Status().StartAsync($"Compiling {projectFile.Name}...", async _ =>
        {
            console.MarkupLine($"Compile project [aqua]{projectFile.FullName}[/].");
            using var workspace = MSBuildWorkspace.Create();
            console.WriteLine("Load project.");
            var project = await workspace.OpenProjectAsync(projectFile.FullName);
            console.MarkupLine("[green]Project loaded.[/]");
            console.WriteLine("Load compilation context.");
            var compilation = await project.GetCompilationAsync();
            console.MarkupLine("[green]Compilation context loaded.[/]");
            if (compilation is null)
            {
                throw new AggregateException("Compilation could not be found.");
            }

            using var assemblyStream = new MemoryStream();
            console.WriteLine("Start compilation.");
            switch (compilation.Emit(assemblyStream))
            {
                case { Success: false, Diagnostics: var diag }:
                    throw new AggregateException(
                        $"Compilation failed: {diag.Aggregate(new StringBuilder(), (sb, d) => sb.AppendLine(d.ToString()))}");
            }

            console.MarkupLine("[green]Compilation successful.[/]");
            console.WriteLine();
            return new AssemblyParser(Assembly.Load(assemblyStream.ToArray()));
        });

    public static Task<AssemblyParser> ForSolution(
        IAnsiConsole console,
        FileInfo slnFile,
        Regex? projectFilter = null,
        string? tfm = null)
        => console.Status().StartAsync($"Compiling {slnFile.Name}...", async _ =>
        {
            projectFilter ??= DefaultRegex();
            tfm ??= "latest";

            console.MarkupLine($"Compile solution [aqua]{slnFile.FullName}[/].");
            console.MarkupLine($"[grey]With project filter:[/] {projectFilter}");
            console.MarkupLine($"[grey]With Target Platform:[/] {tfm}");

            using var workspace = MSBuildWorkspace.Create();
            console.WriteLine("Load solution.");
            var solution = await workspace.OpenSolutionAsync(slnFile.FullName);
            console.MarkupLine("[green]Solution loaded.[/]");

            var assemblies = await Task.WhenAll(solution.Projects
                .Select(p =>
                {
                    var name = TfmComparer.TfmRegex().Replace(p.Name, string.Empty);
                    var tfm = TfmComparer.TfmRegex().Match(p.Name).Groups["tfm"].Value;
                    return (name, tfm, project: p);
                })
                .Where(p => projectFilter.IsMatch(p.name))
                .Where(p => tfm == "latest" || p.tfm.Length == 0 || p.tfm == tfm)
                .OrderByDescending(p => p.tfm, new TfmComparer())
                .GroupBy(p => p.name)
                .Select(p => p.FirstOrDefault())
                .Where(p => p != default)
                .Select(async p =>
                {
                    console.MarkupLine(
                        $"Load compilation context for [aqua]{p.name}[/]{(p.tfm.Length > 0 ? $" [grey]{p.tfm}[/]" : string.Empty)}.");
                    var compilation = await p.project.GetCompilationAsync();
                    console.MarkupLine($"[green]Compilation context loaded for {p.name}.[/]");
                    if (compilation is null)
                    {
                        throw new AggregateException("Compilation could not be found.");
                    }

                    using var assemblyStream = new MemoryStream();
                    console.MarkupLine(
                        $"Start compilation for [aqua]{p.name}[/]{(p.tfm.Length > 0 ? $" [grey]{p.tfm}[/]" : string.Empty)}.");
                    switch (compilation.Emit(assemblyStream))
                    {
                        case { Success: false, Diagnostics: var diag }:
                            throw new AggregateException(
                                $"Compilation failed: {diag.Aggregate(new StringBuilder(), (sb, d) => sb.AppendLine(d.ToString()))}");
                    }

                    console.MarkupLine($"[green]Compilation successful for {p.name}.[/]");
                    return Assembly.Load(assemblyStream.ToArray());
                }));

            console.WriteLine();
            return new AssemblyParser(assemblies);
        });

    public IEnumerable<Type> Entities() =>
        _assemblies
            .SelectMany(a => a.DefinedTypes)
            .Where(t => t.GetCustomAttributes<KubernetesEntityAttribute>().Any())
            .Where(type => !type.GetCustomAttributes<IgnoreAttribute>().Any());

    public IEnumerable<RbacAttribute> RbacAttributes()
    {
        foreach (var type in _assemblies
                     .SelectMany(a => a.DefinedTypes)
                     .SelectMany(t =>
                         t.GetCustomAttributes<GenericRbacAttribute>()))
        {
            yield return type;
        }

        foreach (var type in _assemblies
                     .SelectMany(a => a.DefinedTypes)
                     .SelectMany(t =>
                         t.GetCustomAttributes<EntityRbacAttribute>()))
        {
            yield return type;
        }
    }

    [GeneratedRegex(".*")]
    private static partial Regex DefaultRegex();
}
