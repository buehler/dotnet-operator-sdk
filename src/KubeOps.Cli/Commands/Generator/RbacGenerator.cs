using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.Invocation;

using KubeOps.Abstractions.Kustomize;
using KubeOps.Cli.Output;
using KubeOps.Cli.Roslyn;

using Spectre.Console;

namespace KubeOps.Cli.Commands.Generator;

internal static class RbacGenerator
{
    public static Command Command
    {
        get
        {
            var cmd = new Command("rbac", "Generates rbac roles for the operator project or solution.")
            {
                Options.OutputFormat,
                Options.OutputPath,
                Options.SolutionProjectRegex,
                Options.TargetFramework,
                Arguments.SolutionOrProjectFile,
            };
            cmd.AddAlias("r");
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
            { Extension: ".csproj", Exists: true } => await AssemblyParser.ForProject(console, file),
            { Extension: ".sln", Exists: true } => await AssemblyParser.ForSolution(
                console,
                file,
                ctx.ParseResult.GetValueForOption(Options.SolutionProjectRegex),
                ctx.ParseResult.GetValueForOption(Options.TargetFramework)),
            { Exists: false } => throw new FileNotFoundException($"The file {file.Name} does not exist."),
            _ => throw new NotSupportedException("Only *.csproj and *.sln files are supported."),
        };
        var result = new ResultOutput(console, format);
        console.WriteLine($"Generate RBAC roles for {file.Name}.");
        result.Add("file.yaml", Transpiler.Rbac.Transpile(parser.RbacAttributes()));

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
