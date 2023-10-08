using System.CommandLine;
using System.CommandLine.Invocation;

using KubeOps.Abstractions.Kustomize;
using KubeOps.Cli.Output;
using KubeOps.Cli.Transpilation;

using Spectre.Console;

namespace KubeOps.Cli.Commands.Generator;

internal static class CrdGenerator
{
    public static Command Command
    {
        get
        {
            var cmd = new Command("crds", "Generates CRDs for Kubernetes based on a solution or project.")
            {
                Options.OutputFormat,
                Options.OutputPath,
                Options.SolutionProjectRegex,
                Options.TargetFramework,
                Arguments.SolutionOrProjectFile,
            };
            cmd.SetHandler(ctx => Handler(AnsiConsole.Console, ctx));

            return cmd;
        }
    }

    internal static async Task Handler(IAnsiConsole console, InvocationContext ctx)
    {
        var file = ctx.ParseResult.GetValueForArgument(Arguments.SolutionOrProjectFile);
        var outPath = ctx.ParseResult.GetValueForOption(Options.OutputPath);
        var format = ctx.ParseResult.GetValueForOption(Options.OutputFormat);

        var parser = file switch
        {
            { Extension: ".csproj", Exists: true } => await AssemblyLoader.ForProject(console, file),
            { Extension: ".sln", Exists: true } => await AssemblyLoader.ForSolution(
                console,
                file,
                ctx.ParseResult.GetValueForOption(Options.SolutionProjectRegex),
                ctx.ParseResult.GetValueForOption(Options.TargetFramework)),
            { Exists: false } => throw new FileNotFoundException($"The file {file.Name} does not exist."),
            _ => throw new NotSupportedException("Only *.csproj and *.sln files are supported."),
        };
        var result = new ResultOutput(console, format);

        console.WriteLine($"Generate CRDs for {file.Name}.");
        var crds = parser.Transpile(parser.GetEntities()).ToList();
        foreach (var crd in crds)
        {
            result.Add($"{crd.Metadata.Name.Replace('.', '_')}.{format.ToString().ToLowerInvariant()}", crd);
        }

        result.Add(
            $"kustomization.{format.ToString().ToLowerInvariant()}",
            new KustomizationConfig
            {
                Resources = crds
                    .ConvertAll(crd => $"{crd.Metadata.Name.Replace('.', '_')}.{format.ToString().ToLower()}"),
                CommonLabels = new Dictionary<string, string> { { "operator-element", "crd" } },
            });

        if (outPath is not null)
        {
            await result.Write(outPath);
        }
        else
        {
            result.Write();
        }
    }
}
