using k8s.Models;

using McMaster.Extensions.CommandLineUtils;

using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;

namespace KubeOps.Cli.Generator;

[Command("crd", "crds", Description = "Generates the needed CRD for kubernetes. (Aliases: crds)")]
internal class CrdGenerator
{
    [Argument(0)]
    public string? ProjectFile { get; }

    public async Task<int> OnExecuteAsync(CommandLineApplication app)
    {
        await app.Out.WriteLineAsync("ProjectFIle: " + ProjectFile);
        MSBuildLocator.RegisterDefaults();
        var ws = MSBuildWorkspace.Create();
        var project = await ws.OpenProjectAsync(ProjectFile!);

        foreach (var doc in project.Documents.Where(d => d.SupportsSemanticModel && d.SupportsSyntaxTree))
        {
            var root = await doc.GetSyntaxRootAsync();
            if (root is null)
            {
                continue;
            }

            var entityClasses = root.DescendantNodes().OfType<ClassDeclarationSyntax>()
                .Where(c => c is { AttributeLists.Count: > 0 } && c.AttributeLists.SelectMany(a => a.Attributes)
                    .Any(a => a.Name.ToString() == "KubernetesEntity")).ToList();
        }

        return ExitCodes.Success;
    }
}
