using System.CommandLine;
using System.CommandLine.Invocation;

using k8s;
using k8s.Autorest;
using k8s.Models;

using KubeOps.Cli.Transpilation;
using KubeOps.Transpiler;

using Spectre.Console;

namespace KubeOps.Cli.Commands.Management;

internal static class Uninstall
{
    public static Command Command
    {
        get
        {
            var cmd =
                new Command("uninstall", "Uninstall CRDs from the cluster of the actually selected context.")
                {
                    Options.Force,
                    Options.SolutionProjectRegex,
                    Options.TargetFramework,
                    Arguments.SolutionOrProjectFile,
                };
            cmd.AddAlias("u");
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

        console.WriteLine($"Uninstall CRDs from {file.Name}.");
        var crds = parser.Transpile(parser.GetEntities()).ToList();
        if (crds.Count == 0)
        {
            console.WriteLine("No CRDs found. Exiting.");
            ctx.ExitCode = ExitCodes.Success;
            return;
        }

        console.WriteLine($"Found {crds.Count} CRDs.");
        if (!force && !await console.ConfirmAsync("[red]Should the CRDs be uninstalled?[/]", false))
        {
            ctx.ExitCode = ExitCodes.Aborted;
            return;
        }

        console.WriteLine($"""Starting uninstall from cluster with url "{client.BaseUri}".""");

        foreach (var crd in crds)
        {
            console.MarkupLineInterpolated(
                $"""Uninstall [cyan]"{crd.Spec.Group}/{crd.Spec.Names.Kind}"[/] from the cluster.""");

            try
            {
                switch (await client.ApiextensionsV1.ListCustomResourceDefinitionAsync(
                            fieldSelector: $"metadata.name={crd.Name()}"))
                {
                    case { Items: [var existing] }:
                        await client.ApiextensionsV1.DeleteCustomResourceDefinitionAsync(existing.Name());
                        console.MarkupLineInterpolated(
                            $"""[green]CRD "{crd.Spec.Group}/{crd.Spec.Names.Kind}" deleted.[/]""");
                        break;
                    default:
                        console.MarkupLineInterpolated(
                            $"""[green]CRD "{crd.Spec.Group}/{crd.Spec.Names.Kind}" did not exist.[/]""");
                        break;
                }
            }
            catch (HttpOperationException)
            {
                console.WriteLine(
                    $"""[red]There was a http (api) error while uninstalling "{crd.Spec.Group}/{crd.Spec.Names.Kind}".[/]""");
                throw;
            }
            catch (Exception)
            {
                console.WriteLine(
                    $"""[red]There was an error while uninstalling "{crd.Spec.Group}/{crd.Spec.Names.Kind}".[/]""");
                throw;
            }
        }
    }
}
