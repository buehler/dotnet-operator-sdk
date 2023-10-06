using System.CommandLine;
using System.CommandLine.Invocation;

using k8s;
using k8s.Models;

using KubeOps.Abstractions.Kustomize;
using KubeOps.Cli.Output;

using Spectre.Console;

namespace KubeOps.Cli.Commands.Generator;

internal static class InstallerGenerator
{
    public static Command Command
    {
        get
        {
            var cmd = new Command("installer", "Generates Kustomization YAML to install the entire operator.")
            {
                Options.OutputPath, Options.OutputFormat, Arguments.OperatorName,
            };
            cmd.SetHandler(ctx => Handler(AnsiConsole.Console, ctx));

            return cmd;
        }
    }

    internal static async Task Handler(IAnsiConsole console, InvocationContext ctx)
    {
        var outPath = ctx.ParseResult.GetValueForOption(Options.OutputPath);
        var format = ctx.ParseResult.GetValueForOption(Options.OutputFormat);
        var name = ctx.ParseResult.GetValueForArgument(Arguments.OperatorName);

        var result = new ResultOutput(console, format);
        console.WriteLine("Generate operator installer.");

        result.Add(
            $"namespace.{format.ToString().ToLowerInvariant()}",
            new V1Namespace(metadata: new(name: "system")).Initialize());
        result.Add(
            $"kustomization.{format.ToString().ToLowerInvariant()}",
            new KustomizationConfig
            {
                NamePrefix = $"{name}-",
                Namespace = $"{name}-system",
                CommonLabels = new Dictionary<string, string> { { "operator", name }, },
                Resources = new List<string>
                {
                    $"./namespace.{format.ToString().ToLowerInvariant()}", "./rbac", "./operator", "./crds",
                },
                Images = new List<KustomizationImage>
                {
                    new() { Name = "operator", NewName = "accessible-docker-image", NewTag = "latest", },
                },
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
