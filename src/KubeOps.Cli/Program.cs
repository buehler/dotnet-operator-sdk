using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;

using KubeOps.Cli;
using KubeOps.Cli.Commands.Generator;
using KubeOps.Cli.Commands.Management;

using Spectre.Console;

using Version = KubeOps.Cli.Commands.Utilities.Version;

return await new CommandLineBuilder(new RootCommand(
        "CLI for KubeOps. Commandline tool to help with management tasks such as generating or installing CRDs.")
    {
        Generate.Command, Version.Command, Install.Command, Uninstall.Command,
    })
    .UseDefaults()
    .UseParseErrorReporting(ExitCodes.UsageError)
    .UseExceptionHandler((ex, ctx) =>
    {
        AnsiConsole.MarkupLineInterpolated(
            $"[red]An error occurred whiled executing {ctx.ParseResult.CommandResult.Command}[/]");
        AnsiConsole.MarkupLineInterpolated($"[red]{ex.Message}[/]");
        ctx.ExitCode = ExitCodes.Error;
    })
    .Build()
    .InvokeAsync(args);
