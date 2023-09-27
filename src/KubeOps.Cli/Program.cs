using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;

using KubeOps.Cli;
using KubeOps.Cli.Commands.Generator;

using Spectre.Console;

using Version = KubeOps.Cli.Commands.Utilities.Version;

await new CommandLineBuilder(new RootCommand(
        "CLI for KubeOps. Commandline tool to help with management tasks such as generating or installing CRDs.")
    {
        Generator.Command, Version.Command,
    })
    .UseDefaults()
    .UseParseErrorReporting(ExitCodes.UsageError)
    .UseExceptionHandler((ex, ctx) =>
    {
        AnsiConsole.MarkupLine($"[red]An error ocurred whiled executing {ctx.ParseResult.CommandResult.Command}[/]");
        AnsiConsole.WriteException(ex);
    })
    .Build()
    .InvokeAsync(args);
