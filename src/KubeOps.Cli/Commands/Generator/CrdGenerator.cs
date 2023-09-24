using KubeOps.Cli.Output;
using KubeOps.Cli.SyntaxObjects;

using McMaster.Extensions.CommandLineUtils;

namespace KubeOps.Cli.Commands.Generator;

[Command("crd", "crds", Description = "Generates the needed CRD for kubernetes. (Aliases: crds)")]
internal class CrdGenerator
{
    private readonly ConsoleOutput _output;
    private readonly ResultOutput _result;

    public CrdGenerator(ConsoleOutput output, ResultOutput result)
    {
        _output = output;
        _result = result;
    }

    [Option(
        Description = "The path the command will write the files to. If empty, prints output to console.",
        LongName = "out")]
    public string? OutputPath { get; set; }

    [Option(
        CommandOptionType.SingleValue,
        Description = "Sets the output format for the generator.")]
    public OutputFormat Format { get; set; }

    [Argument(
        0,
        Description =
            "Path to a *.csproj file to generate the CRD from. " +
            "If omitted, the current directory is searched for one and the command fails if none is found.")]
    public string? ProjectFile { get; set; }

    public async Task<int> OnExecuteAsync()
    {
        _result.Format = Format;
        var projectFile = ProjectFile ??
                          Directory.EnumerateFiles(
                                  Directory.GetCurrentDirectory(),
                                  "*.csproj")
                              .FirstOrDefault();
        if (projectFile == null)
        {
            _output.WriteLine(
                "No *.csproj file found. Either specify one or run the command in a directory with one.",
                ConsoleColor.Red);
            return ExitCodes.Error;
        }

        _output.WriteLine($"Generate CRDs from project: {projectFile}.");

        var parser = new ProjectParser(projectFile);
        await foreach (var crd in parser.Entities().Select(Transpiler.Crds.Transpile))
        {
            _result.Add($"{crd.Metadata.Name.Replace('.', '_')}.{Format.ToString().ToLowerInvariant()}", crd);
        }

        if (OutputPath is not null)
        {
            await _result.Write(OutputPath);
        }
        else
        {
            _result.Write();
        }

        return ExitCodes.Success;
    }
}
