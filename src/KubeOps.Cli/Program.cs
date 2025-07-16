// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
