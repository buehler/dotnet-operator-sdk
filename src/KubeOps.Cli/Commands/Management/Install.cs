using System.CommandLine;
using System.CommandLine.Invocation;

using k8s;
using k8s.Autorest;
using k8s.Models;

using KubeOps.Cli.Transpilation;

using Spectre.Console;

namespace KubeOps.Cli.Commands.Management;

internal static class Install
{
    public static Command Command
    {
        get
        {
            var cmd =
                new Command("install", "Install CRDs into the cluster of the actually selected context.")
                {
                    Options.Force,
                    Options.SolutionProjectRegex,
                    Options.TargetFramework,
                    Arguments.SolutionOrProjectFile,
                };
            cmd.AddAlias("i");
            cmd.SetHandler(ctx => Handler(
                AnsiConsole.Console,
                new Kubernetes(KubernetesClientConfiguration.BuildDefaultConfig()),
                ctx));

            return cmd;
        }
    }

    internal static async Task Handler(IAnsiConsole console, IKubernetes client, InvocationContext ctx)
    {
        var file = ctx.ParseResult.GetValueForArgument(Arguments.SolutionOrProjectFile);
        var force = ctx.ParseResult.GetValueForOption(Options.Force);

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

        console.WriteLine($"Install CRDs from {file.Name}.");
        var crds = parser.Transpile(parser.GetEntities()).ToList();
        if (crds.Count == 0)
        {
            console.WriteLine("No CRDs found. Exiting.");
            ctx.ExitCode = ExitCodes.Success;
            return;
        }

        console.WriteLine($"Found {crds.Count} CRDs.");
        console.WriteLine($"""Starting install into cluster with url "{client.BaseUri}".""");

        foreach (var crd in crds)
        {
            console.MarkupLineInterpolated(
                $"""Install [cyan]"{crd.Spec.Group}/{crd.Spec.Names.Kind}"[/] into the cluster.""");

            try
            {
                switch (await client.ApiextensionsV1.ListCustomResourceDefinitionAsync(
                            fieldSelector: $"metadata.name={crd.Name()}"))
                {
                    case { Items: [var existing] }:
                        console.MarkupLineInterpolated(
                            $"""[yellow]CRD "{crd.Spec.Group}/{crd.Spec.Names.Kind}" already exists.[/]""");
                        if (!force && console.Confirm("[yellow]Should the CRD be overwritten?[/]"))
                        {
                            ctx.ExitCode = ExitCodes.Aborted;
                            return;
                        }

                        crd.Metadata.ResourceVersion = existing.ResourceVersion();
                        await client.ApiextensionsV1.ReplaceCustomResourceDefinitionAsync(crd, crd.Name());
                        break;
                    default:
                        await client.ApiextensionsV1.CreateCustomResourceDefinitionAsync(crd);
                        break;
                }

                console.MarkupLineInterpolated(
                    $"""[green]Installed / Updated CRD "{crd.Spec.Group}/{crd.Spec.Names.Kind}".[/]""");
            }
            catch (HttpOperationException)
            {
                console.WriteLine(
                    $"""[red]There was a http (api) error while installing "{crd.Spec.Group}/{crd.Spec.Names.Kind}".[/]""");
                throw;
            }
            catch (Exception)
            {
                console.WriteLine(
                    $"""[red]There was an error while installing "{crd.Spec.Group}/{crd.Spec.Names.Kind}".[/]""");
                throw;
            }
        }
    }
}
