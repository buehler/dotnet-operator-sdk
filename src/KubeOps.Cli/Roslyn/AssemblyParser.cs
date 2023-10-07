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
internal sealed partial class AssemblyParser : IDisposable
{
    private readonly MetadataLoadContext _context;

    static AssemblyParser()
    {
        MSBuildLocator.RegisterDefaults();
    }

    private AssemblyParser(MetadataLoadContext context) => _context = context;

    public static Task<AssemblyParser> ForProject(
        IAnsiConsole console,
        FileInfo projectFile)
        => console.Status().StartAsync($"Compiling {projectFile.Name}...", async _ =>
        {
            console.MarkupLineInterpolated($"Compile project [aqua]{projectFile.FullName}[/].");
            using var workspace = MSBuildWorkspace.Create();
            workspace.SkipUnrecognizedProjects = true;
            workspace.LoadMetadataForReferencedProjects = true;
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
            var mlc = new MetadataLoadContext(
                new PathAssemblyResolver(project.MetadataReferences.Select(m => m.Display ?? string.Empty)));
            mlc.LoadFromByteArray(assemblyStream.ToArray());

            return new AssemblyParser(mlc);
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

            console.MarkupLineInterpolated($"Compile solution [aqua]{slnFile.FullName}[/].");
#pragma warning disable RCS1097
            console.MarkupLineInterpolated($"[grey]With project filter:[/] {projectFilter.ToString()}");
#pragma warning restore RCS1097
            console.MarkupLineInterpolated($"[grey]With Target Platform:[/] {tfm}");

            using var workspace = MSBuildWorkspace.Create();
            workspace.SkipUnrecognizedProjects = true;
            workspace.LoadMetadataForReferencedProjects = true;
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
                    console.MarkupLineInterpolated(
                        $"Load compilation context for [aqua]{p.name}[/]{(p.tfm.Length > 0 ? $" [grey]{p.tfm}[/]" : string.Empty)}.");
                    var compilation = await p.project.GetCompilationAsync();
                    console.MarkupLineInterpolated($"[green]Compilation context loaded for {p.name}.[/]");
                    if (compilation is null)
                    {
                        throw new AggregateException("Compilation could not be found.");
                    }

                    using var assemblyStream = new MemoryStream();
                    console.MarkupLineInterpolated(
                        $"Start compilation for [aqua]{p.name}[/]{(p.tfm.Length > 0 ? $" [grey]{p.tfm}[/]" : string.Empty)}.");
                    switch (compilation.Emit(assemblyStream))
                    {
                        case { Success: false, Diagnostics: var diag }:
                            throw new AggregateException(
                                $"Compilation failed: {diag.Aggregate(new StringBuilder(), (sb, d) => sb.AppendLine(d.ToString()))}");
                    }

                    console.MarkupLineInterpolated($"[green]Compilation successful for {p.name}.[/]");
                    return Assembly.Load(assemblyStream.ToArray());
                }));

            console.WriteLine();
            return new AssemblyParser(new MetadataLoadContext(new PathAssemblyResolver(new string[] { })));
        });

    public IEnumerable<Type> Entities() =>
        _context
            .GetAssemblies()
            .SelectMany(a => a.DefinedTypes)
            .Select(t => (t, attrs: CustomAttributeData.GetCustomAttributes(t)))
            .Where(e => e.attrs.Any(a => a.AttributeType.Name == nameof(KubernetesEntityAttribute)) &&
                        e.attrs.All(a => a.AttributeType.Name != nameof(IgnoreAttribute)))
            .Select(e => e.t);

    public IEnumerable<RbacAttribute> RbacAttributes()
    {
        foreach (var type in _context
                     .GetAssemblies()
                     .SelectMany(a => a.DefinedTypes)
                     .SelectMany(t =>
                         t.GetCustomAttributes<GenericRbacAttribute>()))
        {
            yield return type;
        }

        foreach (var type in _context
                     .GetAssemblies()
                     .SelectMany(a => a.DefinedTypes)
                     .SelectMany(t =>
                         t.GetCustomAttributes<EntityRbacAttribute>()))
        {
            yield return type;
        }
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [GeneratedRegex(".*")]
    private static partial Regex DefaultRegex();
}
